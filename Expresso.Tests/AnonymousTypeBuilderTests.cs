namespace Expresso.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using FluentAssertions;
    using Xunit;

    [Trait("Category", "Expresso - AnonymousTypes")]
    public class AnonymousTypeBuilderTests
    {
        [Fact(DisplayName = "ѕостроение типа с простыми свойствами")]
        public void ShouldCreateTypeWithSimpleProperties()
        {
            var properties = new Dictionary<string, Type> {
                                                              { "Id", typeof(long) },
                                                              { "Link", typeof(string) },
                                                              { "DateStart", typeof(DateTime) },
                                                              { "HasData", typeof(bool) }
                                                          };

            Type type = null;
            Action action = () => type = AnonymousTypeBuilder.Build(properties);

            action.ShouldNotThrow();
            type.Should().NotBeNull();

            var typeProperties = type.GetProperties();
            typeProperties.Should().HaveCount(4);
            typeProperties.All(x => properties.ContainsKey(x.Name)).Should().BeTrue();

        }
    }
}