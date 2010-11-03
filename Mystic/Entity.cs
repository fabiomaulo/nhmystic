namespace Mystic
{
	public class Entity: AbstractEntity<int>
	{
		private int id;

		#region Overrides of AbstractEntity<int>

		public override int Id
		{
			get { return id; }
			protected set { id= value; }
		}

		#endregion
	}
}