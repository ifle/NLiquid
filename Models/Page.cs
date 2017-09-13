using System;

namespace Models
{
	public class Page
	{
		public string Title { get; }

		public Page(string title)
		{
			Title = title ?? throw new ArgumentNullException(nameof(title));
		}
	}
}
