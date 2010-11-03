using System;

namespace Mystic
{
	public abstract class AbstractEntity<TId> : IEntity<TId>, IEquatable<AbstractEntity<TId>>
	{
		#region Implementation of IEntity<TIdentity>

		public abstract TId Id { get; protected set; }

		#endregion

		#region Implementation of IEquatable<IEntity<TIdentity>>
		private Type GetUnproxiedType()
		{
			return GetType();
		}

		public virtual bool Equals(AbstractEntity<TId> other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}
			if (ReferenceEquals(this, other))
			{
				return true;
			}

			var otherType = other.GetUnproxiedType();
			var thisType = GetUnproxiedType();
			if (!otherType.IsAssignableFrom(thisType) && !thisType.IsAssignableFrom(otherType))
			{
				return false;
			}

			bool otherIsTransient = other.IsTransient();
			bool thisIsTransient = IsTransient();
			if (otherIsTransient && thisIsTransient)
				return ReferenceEquals(other, this);

			return Equals(Id, other.Id);
		}

		protected bool IsTransient()
		{
			return Equals(Id, default(TId));
		}

		public override bool Equals(object obj)
		{
			var that = obj as AbstractEntity<TId>;
			return Equals(that);
		}

		private int? requestedHashCode;
		public override int GetHashCode()
		{
			if (!requestedHashCode.HasValue)
			{
				requestedHashCode = IsTransient() ? base.GetHashCode() : Id.GetHashCode();
			}
			return requestedHashCode.Value;
		}
		#endregion
	}
}