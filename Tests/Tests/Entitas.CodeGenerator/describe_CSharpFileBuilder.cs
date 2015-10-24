﻿using System;
using Entitas.CodeGenerator;
using NSpec;

class describe_CSharpFileBuilder : nspec {

    bool logResults = false;

    void generates(CSharpFileBuilder cb, string expectedCode) {
        expectedCode = expectedCode.ToUnixLineEndings();
        var result = cb.ToString();
        if (logResults) {
            Console.WriteLine("should:\n'" + expectedCode + "'");
            Console.WriteLine("was:\n'" + result + "'");
        }
        result.should_be(expectedCode);
    }

    void when_building() {

        CSharpFileBuilder cb = null;
        before = () => {
            cb = new CSharpFileBuilder();
        };

        it["creates empty file"] = () => {
            generates(cb, string.Empty);
        };

        it["creates a namespace"] = () => {
            cb.AddNamespace("MyNamespace");
            generates(cb, @"namespace MyNamespace {
}");
        };

        it["creates multiple namespaces"] = () => {
            cb.AddNamespace("MyNamespace1");
            cb.AddNamespace("MyNamespace2");
            generates(cb, @"namespace MyNamespace1 {
}

namespace MyNamespace2 {
}");
        };

        it["ignores namespace when null or empty"] = () => {
            cb.AddNamespace(null);
            cb.AddNamespace(string.Empty);
            generates(cb, string.Empty);
        };

        it["adds usings"] = () => {
            cb.AddUsing("System");
            cb.AddUsing("Entitas");
            cb.AddNamespace("MyNamespace");
            generates(cb, @"using System;
using Entitas;

namespace MyNamespace {
}");
        };

        it["adds an empty class without namespace"] = () => {
            cb.NoNamespace().AddClass("MyClass");
            generates(cb, @"class MyClass {
}");
        };

        context["when added namespace"] = () => {

            NamespaceDescription aNamespace = null;
            before = () => {
                aNamespace = cb.AddNamespace("MyNamespace");
            };

            it["adds and indents a class"] = () => {
                aNamespace.AddClass("MyClass");
                generates(cb, @"namespace MyNamespace {
    class MyClass {
    }
}");
            };

            it["adds and indents multiple classes"] = () => {
                aNamespace.AddClass("MyClass1");
                aNamespace.AddClass("MyClass2");
                generates(cb, @"namespace MyNamespace {
    class MyClass1 {
    }

    class MyClass2 {
    }
}");
            };

            context["when added a class"] = () => {

                ClassDescription cd = null;
                before = () => {
                    cd = aNamespace.AddClass("MyClass");
                };

                it["sets modifiers"] = () => {
                    cd.AddModifier(AccessModifiers.Public).AddModifier(Modifiers.Static);
                    generates(cb, @"namespace MyNamespace {
    public static class MyClass {
    }
}");
                };

                it["adds a field of type"] = () => {
                    cd.AddField(typeof(string), "_name");
                    generates(cb, @"namespace MyNamespace {
    class MyClass {
        string _name;
    }
}");
                };

                it["adds multiple fields of type"] = () => {
                    cd.AddField(typeof(string), "_name");
                    cd.AddField(typeof(int), "_age");
                    generates(cb, @"namespace MyNamespace {
    class MyClass {
        string _name;
        int _age;
    }
}");
                };

                it["adds a method"] = () => {
                    cd.AddMethod("MyMethod", @"var str = ""Hello"";
System.Console.WriteLine(str);");
                    generates(cb, @"namespace MyNamespace {
    class MyClass {
        void MyMethod() {
            var str = ""Hello"";
            System.Console.WriteLine(str);
        }
    }
}");
                };

                it["adds multiple methods"] = () => {
                    cd.AddMethod("MyMethod1", @"var str = ""Hello"";
System.Console.WriteLine(str);");

                    cd.AddMethod("MyMethod2", @"var str = ""World"";
System.Console.WriteLine(str);");

                    generates(cb, @"namespace MyNamespace {
    class MyClass {
        void MyMethod1() {
            var str = ""Hello"";
            System.Console.WriteLine(str);
        }

        void MyMethod2() {
            var str = ""World"";
            System.Console.WriteLine(str);
        }
    }
}");
                };

                context["when field added"] = () => {

                    FieldDescription fd = null;
                    before = () => {
                        fd = cd.AddField(typeof(string), "version");
                    };

                    it["sets modifiers"] = () => {
                        fd.AddModifier(AccessModifiers.Public).AddModifier(Modifiers.Const);
                        generates(cb, @"namespace MyNamespace {
    class MyClass {
        public const string version;
    }
}");
                    };

                    it["sets default value"] = () => {
                        fd.SetValue("\"1.0.0\"");
                        generates(cb, @"namespace MyNamespace {
    class MyClass {
        string version = ""1.0.0"";
    }
}");
                    };

                    it["adds a method"] = () => {
                        cd.AddMethod("MyMethod", @"var str = ""Hello"";
System.Console.WriteLine(str);");

                        generates(cb, @"namespace MyNamespace {
    class MyClass {
        string version;

        void MyMethod() {
            var str = ""Hello"";
            System.Console.WriteLine(str);
        }
    }
}");
                    };
                };

                context["when method added"] = () => {

                    MethodDescription md = null;
                    before = () => {
                        md = cd.AddMethod("MyMethod", "var str = \"Hello\";\nSystem.Console.WriteLine(str);");
                    };

                    it["sets modiefiers"] = () => {
                        md.AddModifier(AccessModifiers.Public).AddModifier(Modifiers.Override);
                        generates(cb, @"namespace MyNamespace {
    class MyClass {
        public override void MyMethod() {
            var str = ""Hello"";
            System.Console.WriteLine(str);
        }
    }
}");
                    };

                    it["sets returnType"] = () => {
                        md.SetReturnType(typeof(char[]));
                        generates(cb, @"namespace MyNamespace {
    class MyClass {
        char[] MyMethod() {
            var str = ""Hello"";
            System.Console.WriteLine(str);
        }
    }
}");
                    };

                    it["sets parameters"] = () => {
                        md.AddParameter(new MethodParameterDescription(typeof(string), "name"))
                            .AddParameter(new MethodParameterDescription(typeof(int), "age"));

                        generates(cb, @"namespace MyNamespace {
    class MyClass {
        void MyMethod(string name, int age) {
            var str = ""Hello"";
            System.Console.WriteLine(str);
        }
    }
}");
                    };

                    it["sets parameters with keyword"] = () => {
                        md.AddParameter(new MethodParameterDescription(typeof(string), "name"))
                            .AddParameter(new MethodParameterDescription(typeof(int), "age").SetKeyword(MethodParameterKeyword.Out));

                        generates(cb, @"namespace MyNamespace {
    class MyClass {
        void MyMethod(string name, out int age) {
            var str = ""Hello"";
            System.Console.WriteLine(str);
        }
    }
}");
                    };

                    it["sets default value"] = () => {
                        md.AddParameter(new MethodParameterDescription(typeof(string), "name"))
                            .AddParameter(new MethodParameterDescription(typeof(int), "age").SetDefaultValue("42"));

                        generates(cb, @"namespace MyNamespace {
    class MyClass {
        void MyMethod(string name, int age = 42) {
            var str = ""Hello"";
            System.Console.WriteLine(str);
        }
    }
}");
                    };
                };
            };
        };
    }
}

