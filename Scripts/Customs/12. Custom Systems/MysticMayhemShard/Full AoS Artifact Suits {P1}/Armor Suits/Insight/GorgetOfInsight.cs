using System;
using Server;

namespace Server.Items
{
	public class GorgetOfInsight : PlateGorget
	{
		public override int LabelNumber{ get{ return 1061096; } } // Gorget of Insight
		public override SetItem SetID{ get{ return SetItem.Insight; } }
		public override int Pieces{ get{ return 5; } }
		public override int ArtifactRarity{ get{ return 11; } }

		public override int BaseEnergyResistance{ get{ return 17; } }

		public override int InitMinHits{ get{ return 255; } }
		public override int InitMaxHits{ get{ return 255; } }

		[Constructable]
		public GorgetOfInsight()
		{
			Name = "Gorget of Insight";
			Hue = 0x554;
			
			SetAttributes.BonusInt = 8;
			SetAttributes.BonusMana = 15;
			SetAttributes.RegenMana = 2;
			SetAttributes.LowerManaCost = 8;
		}

		public GorgetOfInsight( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 1 );
		}
		
		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if ( version < 1 )
				EnergyBonus = 0;
		}
	}
}