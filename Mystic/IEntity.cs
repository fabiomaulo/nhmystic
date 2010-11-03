using System;

namespace Mystic
{
	public interface IEntity<TIdentity>
	{
		TIdentity Id { get; }
	}
}