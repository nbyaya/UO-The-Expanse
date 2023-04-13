using System; 
using Server; 
using Server.Items;
using Server.Gumps;

namespace Server.Items
{
	public class TenkGoldDeed : Item
	{
		[Constructable]
		public TenkGoldDeed() : this( 1 )
		{
			ItemID = 5360;
			Movable = true;
			Hue = 52;
			Name = " A Deed for a 10k BankCheck";					
		}

		public override void OnDoubleClick( Mobile from )
		{
			from.AddToBackpack( new BankCheck(10000) ); 
			this.Delete();
		}

		[Constructable]
		public TenkGoldDeed( int amount ) 
        {
		}		

		public TenkGoldDeed( Serial serial ) : base( serial ) 
		{
		}

		public override void Serialize( GenericWriter writer ) 
		{ 
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		} 

		public override void Deserialize( GenericReader reader ) 
		{ 
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
}