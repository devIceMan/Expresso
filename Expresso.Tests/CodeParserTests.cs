namespace Expresso.Tests
{
    using System;
    using System.Text;
    using Expresso;
    using FluentAssertions;
    using Xunit;

    [Trait("Category", "Expresso - CodeCompiler")]
    public class CodeParserTests
    {
        [Fact]
        public void Should_parse_and_return_class()
        {
            var code = "namespace G{ public class GeneratedClass { public string Name {get;} = \"TheValue\";  } }";
            var parser = new CodeCompiler();
            var asm = parser.CompileToAssembly(code);
            var type = asm.GetType("G.GeneratedClass");

            dynamic instance = Activator.CreateInstance(type);

            string nameValue = instance.Name.ToString();
            nameValue.ShouldBeEquivalentTo("TheValue");
        }

        [Fact]
        public void Should_parse_stringbuilder_result()
        {
            var code = @"namespace G{ 
                    public class GeneratedClass { 
                        public string Name {get;} = new StringBuilder().Append(""123"").ToString();  
                    } 
            }";
            var parser = new CodeCompiler().WithAssemblyOf<StringBuilder>().UsingNamespaceOf<StringBuilder>();
            var asm = parser.CompileToAssembly(code);
            var type = asm.GetType("G.GeneratedClass");

            dynamic instance = Activator.CreateInstance(type);

            string nameValue = instance.Name.ToString();
            nameValue.ShouldBeEquivalentTo("123");
        }

        [Fact]
        public void Should_parse_stringbuilder_method_result()
        {
            var code = @"namespace G{ 
                    public class GeneratedClass { 
                        public string GetName(string prefix){
                            return new StringBuilder().Append(prefix).Append(""123"").ToString();
                        }  
                    } 
            }";

            var parser = new CodeCompiler().WithAssemblyOf<StringBuilder>().UsingNamespaceOf<StringBuilder>();
            var asm = parser.CompileToAssembly(code);
            var type = asm.GetType("G.GeneratedClass");

            dynamic instance = Activator.CreateInstance(type);

            string nameValue = instance.GetName("321").ToString();
            nameValue.ShouldBeEquivalentTo("321123");
        }

        [Fact]
        public void Should_parse_and_return_interface_inmplementation()
        {
            var code = @"namespace G{ 
                    public class GeneratedClass : IHaveName { 
                        public string GetName(string prefix){
                            return new StringBuilder().Append(prefix).Append(""123"").ToString();
                        }  
                    } 
            }";

            var parser = new CodeCompiler()
                .Using<StringBuilder>()
                .Using<IHaveName>();

            var instance = parser.CompileToInstance<IHaveName>(code);                                            
            var nameValue = instance.GetName("321");
            nameValue.ShouldBeEquivalentTo("321123");
        }
    }

    public class CodeCompiler : AbstractCompiler<CodeCompiler>
    {
        
    }

    public interface IHaveName
    {
        string GetName(string prefix);
    }
}