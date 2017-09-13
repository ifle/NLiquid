using System;

namespace Models
{
	public class Product
	{
		public string Title { get; }

		public Product(string title)
		{
			Title = title ?? throw new ArgumentNullException(nameof(title));
		}
	}
}
