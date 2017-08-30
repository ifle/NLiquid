//using System.Diagnostics;
//using Nitra;
//using NLiquid.Parser.Syntax;
//using NUnit.Framework;
//using System.ComponentModel;
//using System;
//using System.Linq;
//using System.Reflection;
//using NLiquid.Filters;

//namespace NLiquid.Tests
//{
//	[TestFixture]
//	public class Class1
//    {
//		[Test]
//		public void  WillbeRemoved()
//	    {
//		    foreach (var filterType in Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsClass && type.GetCustomAttribute<FilterContainerAttribute>() != null))
//		    foreach (var method in filterType.GetMethods(BindingFlags.Public | BindingFlags.Static))
//			{
//				var nameAttribute = method.GetCustomAttribute<FilterAttribute>();
//				Console.WriteLine($"Method = {method.Name}, Name={nameAttribute?.Name}");
//			}
//	    }
//    }
//}

//namespace NLiquid.Filters
//{
//	[FilterContainer]
//	public class Filters
//	{
//		[Filter("round")]
//		public static double Round(double val, int digits)
//		{
//			return Math.Round(val, digits);
//		}
//	}

//	public class FilterAttribute: Attribute
//	{
//		public FilterAttribute(string name)
//		{
//			Name = name;
//		}

//		public string Name { get; private set; }
//	}

//	public class FilterContainerAttribute : Attribute
//	{

//	}
//}
