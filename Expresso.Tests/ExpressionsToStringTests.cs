namespace Expresso.Tests {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text;
    using Expresso;
    using FluentAssertions;
    using Xunit;

    [Trait("Category", "Expresso - ToString")]
    public class ExpressionsToStringTests : ExpressionsTestBase
    {
        [Fact]
        public void Should_Serialize_Int_Constants()
        {
            Expression<Func<int, int, int>> expression = (a, b) => a + b;
            var str = ExpressionParser.ToString(expression);
            str.CompareWithoutBomAndWhitespace("(a, b) => a + b");
        }

        [Fact]
        public void Should_Serialize_Char_Constants()
        {
            Expression<Func<string>> expression = () => "" + 'a' + 'b';
            var str = ExpressionParser.ToString(expression);
            str.CompareWithoutBomAndWhitespace("() => \"\" + (object)'a' + (object)'b'");
        }

        [Fact]
        public void Should_Serialize_DateTime_Now()
        {
            Expression<Func<DateTime>> expression = () => DateTime.Now;
            var str = ExpressionParser.ToString(expression);
            str.CompareWithoutBomAndWhitespace("() => DateTime.Now");
        }

        [Fact]
        public void Should_Serialize_DateTime()
        {
            Expression<Func<DateTime>> expression = () => new DateTime(2017, 4, 19);
            var str = ExpressionParser.ToString(expression);
            str.CompareWithoutBomAndWhitespace("() => new System.DateTime(2017, 4, 19)");
        }

        //[Fact]
        //public void Should_Serialize_Generic_Method()
        //{
        //    Expression<Func<DynamicDictionary, bool>> expr = d => d.GetAs<int>("A", 12, false) == 12;
        //    var str = ExpressionParser.ToString(expr);
        //    str.CompareWithoutBomAndWhitespace("d => d.GetAs<int>(\"A\", 12, false) == 12");
        //}

        //[Fact]
        //public void Should_Serialize_Extension_Method_Call()
        //{
        //    Expression<Func<DynamicDictionary, bool>> expr = d => d.Get("A", 0) == 12;
        //    var str = ExpressionParser.ToString(expr);
        //    str.CompareWithoutBomAndWhitespace("d => d.Get(\"A\", 0) == 12");
        //}

        [Fact]
        public void Should_Serialize_Static_Method_Call()
        {
            Expression<Func<object, bool>> expr = d => int.Parse("12") == 12;
            var str = ExpressionParser.ToString(expr);
            str.CompareWithoutBomAndWhitespace("d => int.Parse(\"12\") == 12");
        }

        [Fact]
        public void Should_Inline_Closure_With_Int_Const()
        {
            var stringVal = "12";
            Expression<Func<object, bool>> expr = d => int.Parse(stringVal) == 12;
            var str = ExpressionParser.ToString(expr);
            str.CompareWithoutBomAndWhitespace("d => int.Parse(\"12\") == 12");
        }

        [Fact]
        public void Should_Inline_Closure_With_DateTime_Const()
        {
            var dateTime = new DateTime(2017, 04, 21, 12, 38, 54);            
            Expression<Func<object, bool>> expr = d => dateTime.Date == DateTime.Today;
            var str = ExpressionParser.ToString(expr);
            str.CompareWithoutBomAndWhitespace($"d => new DateTime({dateTime.Ticks}, System.DateTimeKind.Unspecified).Date == DateTime.Today");
        }

        [Fact]
        public void Should_Inline_Closure_With_Timespan_Const()
        {
            var timespan = TimeSpan.FromSeconds(5);            
            Expression<Func<object, bool>> expr = d => timespan == TimeSpan.FromSeconds(5);
            var str = ExpressionParser.ToString(expr);
            str.CompareWithoutBomAndWhitespace($"d => TimeSpan.FromTicks({timespan.Ticks}) == TimeSpan.FromSeconds(5)");
        }

        [Fact]
        public void Should_Inline_Closure_With_Anonymous_Type_Const()
        {
            var @object = new {
                X = 1,
                Y = TimeSpan.FromSeconds(5)
            };
            Expression<Func<object, bool>> expr = d => @object != null;
            var str = ExpressionParser.ToString(expr);
            str.CompareWithoutBomAndWhitespace($"d => new {{ X = 1, Y = TimeSpan.FromTicks(50000000) }} != null");
        }

        [Fact]
        public void Should_Inline_Closure_With_Instance_With_Default_Ctor_Const()
        {
            var @object = new TypeToInline {
                Count = 10,
                Name = "The name of the instance",
                Quantity = 20.75                
            };
            Expression<Func<object, bool>> expr = d => @object != null;
            var str = ExpressionParser.ToString(expr);
            str.CompareWithoutBomAndWhitespace($"d => new {typeof(TypeToInline).FullName} {{ Count = 10, Name = \"The name of the instance\", Quantity = 20.75 }} != null");
        }

        //[Fact]
        //public void Should_Inline_Closure_With_DynamicDictionary_Const()
        //{
        //    var @object = DynamicDictionary.Create()
        //        .SetValue("Count", 10)
        //        .SetValue("Name", "The name of the instance")
        //        .SetValue("Quantity", 20.75);

        //    Expression<Func<DynamicDictionary, bool>> expr = d => @object != null;
        //    var str = ExpressionParser.ToString(expr);
        //    str.CompareWithoutBomAndWhitespace("d => DynamicDictionary.Create().SetValue(\"Count\", 10).SetValue(\"Name\", \"The name of the instance\").SetValue(\"Quantity\", 20.75) != null");
        //}

        [Fact]
        public void Should_Inline_Closure_With_Struct_Const()
        {
            StructToInline? @object = new StructToInline {
                Name = "Name of the struct",
                Count = 12
            };

            StructToInlineWithCtor? @struct = new StructToInlineWithCtor("Name of the struct", 12);

            Expression<Func<object, bool>> expr = d => @object != null && @struct != null;
            var str = ExpressionParser.ToString(expr);
            str.CompareWithoutBomAndWhitespace($"d => new {typeof(StructToInline).FullName} {{ Name = \"Name of the struct\", Count = 12 }} != null && new {typeof(StructToInlineWithCtor).FullName}(\"Name of the struct\", 12) != null");                     
        }

        [Fact]
        public void Should_Serialize_Simple_Linq()
        {
            Expression<Func<IList<StructToInline>, bool>> expr = list => list.Where(x => x.Count > 0).Any(a => a.Name == null);
            var str = ExpressionParser.ToString(expr);
            str.CompareWithoutBomAndWhitespace("list => list.Where(x => x.Count > 0).Any(a => a.Name == null)");
        }

        [Fact]
        public void Should_Inline_Simple_List()
        {
            var list = new List<StructToInline> {
                new StructToInline {
                    Name = "N1",
                    Count = 1
                },
                new StructToInline {
                    Name = "N2",
                    Count = 2
                },
                new StructToInline {
                    Name = "N3",
                    Count = 3
                },
                new StructToInline {
                    Name = "N4",
                    Count = 4
                }
            };
            
            Expression<Func<bool>> expr = () => list.Where(x => x.Count > 0).Any(a => a.Name == null);
            var expect = "() => new System.Collections.Generic.List<Expresso.UnitTests.StructToInline> { new Expresso.UnitTests.StructToInline { Name = \"N1\", Count = 1 }, new Expresso.UnitTests.StructToInline { Name = \"N2\", Count = 2 }, new Expresso.UnitTests.StructToInline { Name = \"N3\", Count = 3 }, new Expresso.UnitTests.StructToInline { Name = \"N4\", Count = 4 } }.Where(x => x.Count > 0).Any(a => a.Name == null)";
            var str = ExpressionParser.ToString(expr);
            str.CompareWithoutBomAndWhitespace(expect);
        }
    }

    public struct StructToInline
    {
        public string Name { get; set; }

        public int Count { get; set; }
    }

    public struct StructToInlineWithCtor
    {
        public StructToInlineWithCtor(string name, int count)
        {
            Name = name;
            Count = count;
        }

        public string Name { get; set; }

        public int Count { get; set; }
    }

    public class TypeToInline
    {
        public string Name { get; set; }

        public double Quantity { get; set; }

        public int Count { get; set; }
    }

    public static class StringExtensions
    {
        public static string NoWhitespace(this string str)
        {
            return str.Replace(" ", "");
        }

        public static string NoBom(this string str)
        {
            var bomMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            if (str.StartsWith(bomMarkUtf8))
                str = str.Remove(0, bomMarkUtf8.Length);
            return str;
        }

        public static void CompareWithoutBomAndWhitespace(this string value, string expected)
        {
            value.NoBom().NoWhitespace().Should().Be(expected.NoBom().NoWhitespace());
        }
    }
}