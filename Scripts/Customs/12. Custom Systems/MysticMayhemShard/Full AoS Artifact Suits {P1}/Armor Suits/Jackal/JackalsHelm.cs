using System;
using Server;

namespace Server.Items
{
	public class JackalsHelm : PlateHelm
	{
		public override int LabelNumber{ get{ return 1061594; } } // Jackal's Helm
		public override SetItem SetID{ get{ return SetItem.Jackal; } }
		public override int Pieces{ get{ return 5; } }
		public override int ArtifactRarity{ get{ return 11; } }

		public override int BaseFireResistance{ get{ return 9; } }
		public override int BaseColdResistance{ get{ return 9; } }

		public override int InitMinHits{ get{ return 255; } }
		public override int InitMaxHits{ get{ return 255; } }

		[Constructable]
		public JackalsHelm()
		{
			Name = "Jackal's Helm";
			Hue = 0x6D1;
			
			SetAttributes.BonusDex = 15;
			SetAttributes.RegenHits = 2;
		}

		public JackalsHelm( Serial serial ) : base( serial )
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
			{
				if ( Hue == 0x54B )
					Hue = 0x6D1;

				FireBonus = 0;
				ColdBonus = 0;
			}
		}
	}
}