using System;

namespace Models
{
	public class Customer
	{
		public string Name { get; }

		public Customer(string name)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
		}
	}
}
