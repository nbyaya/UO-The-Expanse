using System;
using Server;

namespace Server.Items
{
	public class DivineGorget : LeatherGorget
	{
		public override int LabelNumber{ get{ return 1061289; } } // Divine Gorget
		public override SetItem SetID{ get{ return SetItem.Divine; } }
		public override int Pieces{ get{ return 5; } }

		public override int ArtifactRarity{ get{ return 11; } }

		public override int BasePhysicalResistance{ get{ return 6; } }
		public override int BaseFireResistance{ get{ return 5; } }
		public override int BaseColdResistance{ get{ return 7; } }
		public override int BaseEnergyResistance{ get{ return 20; } }

		public override int InitMinHits{ get{ return 255; } }
		public override int InitMaxHits{ get{ return 255; } }

		[Constructable]
		public DivineGorget()
		{
			Name = "Divine Gorget";
			Hue = 0x482;
			
			SetAttributes.BonusInt = 6;
			SetAttributes.RegenMana = 1;
			SetAttributes.ReflectPhysical = 8;
			SetAttributes.LowerManaCost = 4;
		}

		public DivineGorget( Serial serial ) : base( serial )
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

			switch ( version )
			{
				case 0:
				{
					PhysicalBonus = 0;
					FireBonus = 0;
					ColdBonus = 0;
					EnergyBonus = 0;
					break;
				}
			}
		}
	}
}