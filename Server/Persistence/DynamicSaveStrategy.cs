﻿/***************************************************************************
 *                          DynamicSaveStrategy.cs
 *                            -------------------
 *   begin                : December 16, 2010
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id: DynamicSaveStrategy.cs 645 2010-12-23 11:36:25Z asayre $
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

#if Framework_4_0

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Linq;

using Server;
using Server.Guilds;

namespace Server
{
	public sealed class DynamicSaveStrategy : SaveStrategy
	{
		public override string Name { get { return "Dynamic"; } }

		private SaveMetrics _metrics;

		private SequentialFileWriter _itemData, _itemIndex;
		private SequentialFileWriter _mobileData, _mobileIndex;
		private SequentialFileWriter _guildData, _guildIndex;

		private ConcurrentBag<Item> _decayBag;

		private BlockingCollection<QueuedMemoryWriter> _itemThreadWriters;
		private BlockingCollection<QueuedMemoryWriter> _mobileThreadWriters;
		private BlockingCollection<QueuedMemoryWriter> _guildThreadWriters;

		public DynamicSaveStrategy()
		{
			_decayBag = new ConcurrentBag<Item>();
			_itemThreadWriters = new BlockingCollection<QueuedMemoryWriter>();
			_mobileThreadWriters = new BlockingCollection<QueuedMemoryWriter>();
			_guildThreadWriters = new BlockingCollection<QueuedMemoryWriter>();
		}

		public override void Save(SaveMetrics metrics, bool permitBackgroundWrite)
		{
			this._metrics = metrics;

			OpenFiles();

			Task[] saveTasks = new Task[3];

			saveTasks[0] = SaveItems();
			saveTasks[1] = SaveMobiles();
			saveTasks[2] = SaveGuilds();

			SaveTypeDatabases();

			if (permitBackgroundWrite)
			{
				//This option makes it finish the writing to disk in the background, continuing even after Save() returns.
				Task.Factory.ContinueWhenAll(saveTasks, _ =>
					{
						CloseFiles();

						World.NotifyDiskWriteComplete();
					});
			}
			else
			{
			Task.WaitAll(saveTasks);	//Waits for the completion of all of the tasks(committing to disk)
			CloseFiles();
			}
		}

		private Task StartCommitTask(BlockingCollection<QueuedMemoryWriter> threadWriter, SequentialFileWriter data, SequentialFileWriter index)
		{
			Task commitTask = Task.Factory.StartNew(() =>
			{
				while (!(threadWriter.IsCompleted))
				{
					QueuedMemoryWriter writer;

					try
					{
						writer = threadWriter.Take();
					}
					catch (InvalidOperationException)
					{
						//Per MSDN, it's fine if we're here, successful completion of adding can rarely put us into this state.
						break;
					}

					writer.CommitTo(data, index);
				}
			});

			return commitTask;
		}

		private Task SaveItems()
		{
			//Start the blocking consumer; this runs in background.
			Task commitTask = StartCommitTask(_itemThreadWriters, _itemData, _itemIndex);

			IEnumerable<Item> items = World.Items.Values;

			//Start the producer.
			Parallel.ForEach(items, () => new QueuedMemoryWriter(), 
				(Item item, ParallelLoopState state, QueuedMemoryWriter writer) =>
				{
					long startPosition = writer.Position;

					item.Serialize(writer);

					int size = (int)(writer.Position - startPosition);

					writer.QueueForIndex(item, size);

					if (item.Decays && item.Parent == null && item.Map != Map.Internal && DateTime.Now > (item.LastMoved + item.DecayTime))
					{
						_decayBag.Add(item);
					}

					if (_metrics != null)
					{
						_metrics.OnItemSaved(size);
					}

					return writer;
				},
				(writer) =>
				{
					writer.Flush();

					_itemThreadWriters.Add(writer);
				});

			_itemThreadWriters.CompleteAdding();	//We only get here after the Parallel.ForEach completes.  Lets our task 

			return commitTask;
		}



		private Task SaveMobiles()
		{
			//Start the blocking consumer; this runs in background.
			Task commitTask = StartCommitTask( _mobileThreadWriters, _mobileData, _mobileIndex );

			IEnumerable<Mobile> mobiles = World.Mobiles.Values;

			//Start the producer.
			Parallel.ForEach(mobiles, () => new QueuedMemoryWriter(),
				(Mobile mobile, ParallelLoopState state, QueuedMemoryWriter writer) =>
				{
					long startPosition = writer.Position;

					mobile.Serialize(writer);

					int size = (int)(writer.Position - startPosition);

					writer.QueueForIndex(mobile, size);

					if (_metrics != null)
					{
						_metrics.OnMobileSaved(size);
					}

					return writer;
				},
				(writer) =>
				{
					writer.Flush();

					_mobileThreadWriters.Add(writer);
				});

			_mobileThreadWriters.CompleteAdding();	//We only get here after the Parallel.ForEach completes.  Lets our task tell the consumer that we're done

			return commitTask;
		}

		private Task SaveGuilds()
		{
			//Start the blocking consumer; this runs in background.
			Task commitTask = StartCommitTask(_guildThreadWriters, _guildData, _guildIndex);

			IEnumerable<BaseGuild> guilds = BaseGuild.List.Values;

			//Start the producer.
			Parallel.ForEach(guilds, () => new QueuedMemoryWriter(),
				(BaseGuild guild, ParallelLoopState state, QueuedMemoryWriter writer) =>
				{
					long startPosition = writer.Position;

					guild.Serialize(writer);

					int size = (int)(writer.Position - startPosition );

					writer.QueueForIndex(guild, size);

					if (_metrics != null)
					{
						_metrics.OnGuildSaved(size);
					}

					return writer;
				},
				(writer) =>
				{
					writer.Flush();

					_guildThreadWriters.Add(writer);
				});

			_guildThreadWriters.CompleteAdding();	//We only get here after the Parallel.ForEach completes.  Lets our task 

			return commitTask;
		}

		public override void ProcessDecay()
		{
			Item item;

			while( _decayBag.TryTake( out item ) )
			{
				item.Delete();
			}
		}

		private void OpenFiles()
		{
			_itemData = new SequentialFileWriter(World.ItemDataPath, _metrics);
			_itemIndex = new SequentialFileWriter(World.ItemIndexPath, _metrics);

			_mobileData = new SequentialFileWriter(World.MobileDataPath, _metrics);
			_mobileIndex = new SequentialFileWriter(World.MobileIndexPath, _metrics);

			_guildData = new SequentialFileWriter(World.GuildDataPath, _metrics);
			_guildIndex = new SequentialFileWriter(World.GuildIndexPath, _metrics);

			WriteCount(_itemIndex, World.Items.Count);
			WriteCount(_mobileIndex, World.Mobiles.Count);
			WriteCount(_guildIndex, BaseGuild.List.Count);
		}

		private void CloseFiles()
		{
			_itemData.Close();
			_itemIndex.Close();

			_mobileData.Close();
			_mobileIndex.Close();

			_guildData.Close();
			_guildIndex.Close();
		}

		private void WriteCount(SequentialFileWriter indexFile, int count)
		{
			//Equiv to GenericWriter.Write( (int)count );
			byte[] buffer = new byte[4];

			buffer[0] = (byte)(count);
			buffer[1] = (byte)(count >> 8);
			buffer[2] = (byte)(count >> 16);
			buffer[3] = (byte)(count >> 24);

			indexFile.Write(buffer, 0, buffer.Length);
		}

		private void SaveTypeDatabases()
		{
			SaveTypeDatabase(World.ItemTypesPath, World.m_ItemTypes);
			SaveTypeDatabase(World.MobileTypesPath, World.m_MobileTypes);
		}

		private void SaveTypeDatabase(string path, List<Type> types)
		{
			BinaryFileWriter bfw = new BinaryFileWriter(path, false);

			bfw.Write(types.Count);

			foreach (Type type in types)
			{
				bfw.Write(type.FullName);
			}

			bfw.Flush();

			bfw.Close();
		}
	}

}
#endif