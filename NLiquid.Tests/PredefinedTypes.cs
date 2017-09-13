using System.Diagnostics;
using Nitra;
using NLiquid.Parser.Syntax;
using NUnit.Framework;
using System.ComponentModel;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Nitra.Declarations;
using NLiquid.Filters;
using NLiquid.Parser.Ast;
using Type = System.Type;

namespace NLiquid.Tests
{
	[TestFixture]
	public class PredefinedTypesTest
	{
		[Test]
		public void LoadGlobalTypes_Test()
		{
			// Arrange
			var root = new NamespaceSymbol
			{
				FullName = "<root>",
				Kind = "root"
			};

			// Act
			var types = new NLiquidGlobalTypes(root.MemberTable);

			// Assert
			Assert.NotNull(types);
			CollectionAssert.IsNotEmpty(types.Types);
		}

		[Test]
		public void LoadFilters_Test()
		{
			// Arrange
			var root = new NamespaceSymbol
			{
							FullName = "<root>",
							Kind = "root"
						};
			var globalTypes = new NLiquidGlobalTypes(root.MemberTable);
			// Act
			var filters = FilterTypes.Create(root.MemberTable, globalTypes);

			// Assert
			Assert.NotNull(filters);
			CollectionAssert.IsNotEmpty(filters.Filters);
		}

		[Test]
		public void LoadPredefinedTypes_Test()
		{
			// Arrange
			var root = new NamespaceSymbol
			{
				FullName = "<root>",
				Kind = "root"
			};
			var globalTypes =  new NLiquidGlobalTypes(root.MemberTable);
			var filters = FilterTypes.Create(root.MemberTable, globalTypes);

			// Act
			var predefinedTypes = new PredefinedTypes(globalTypes, filters);

			// Assert
			Assert.NotNull(predefinedTypes.GlobalTypes);
			Assert.NotNull(predefinedTypes.Filters);
			CollectionAssert.IsNotEmpty(predefinedTypes.Symbols);
		}
	}
}
