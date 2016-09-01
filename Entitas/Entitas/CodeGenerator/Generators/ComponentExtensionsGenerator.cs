﻿using System.Collections.Generic;
using System.Linq;
using Entitas.Serialization;

namespace Entitas.CodeGenerator {

    public class ComponentExtensionsGenerator : IComponentCodeGenerator {

        public CodeGenFile[] Generate(ComponentInfo[] componentInfos) {
            var generatorName = GetType().FullName;
            return componentInfos
                .Where(info => info.generateMethods)
                .Select(info => new CodeGenFile(
                    info.fullTypeName + "GeneratedExtension",
                    generateComponentExtension(info),
                    generatorName
                )).ToArray();
        }

        static string generateComponentExtension(ComponentInfo componentInfo) {
            var code = addUsings("Entitas");
            if(componentInfo.generateComponent) {
                code += generateComponent(componentInfo);
            }

            code += addEntityMethods(componentInfo);
            if(componentInfo.isSingleEntity) {
                code += addPoolMethods(componentInfo);
            }

            code += addMatchers(componentInfo);
            return code;
        }

        static string addUsings(params string[] usings) {
            return string.Join("\n", usings
                .Select(name => "using " + name + ";")
                .ToArray()) + "\n";
        }

        static string generateComponent(ComponentInfo componentInfo) {
            const string componentFormat = @"
public class {0} : IComponent {{
    public {1} {2};
}}
";
            var memberInfo = componentInfo.memberInfos[0];
            return string.Format(componentFormat, componentInfo.fullTypeName, memberInfo.type, memberInfo.name);
        }

        /*
         *
         * ENTITY METHODS
         *
         */

        static string addEntityMethods(ComponentInfo componentInfo) {
            var code = string.Empty;
            for(int i = 0; i < componentInfo.pools.Length; i++) {
                code += addEntityClassHeader(componentInfo, i)
                            + addGetMethods(componentInfo, i)
                            + addHasMethods(componentInfo, i)
                            + addAddMethods(componentInfo, i)
                            + addReplaceMethods(componentInfo, i)
                            + addRemoveMethods(componentInfo, i)
                            + addCloseClass();
            }

            return code;
        }

        static string addEntityClassHeader(ComponentInfo componentInfo, int poolIndex) {
            const string classHeader = "\npublic partial class $Tag : Entity {\n";
            return buildString(componentInfo, classHeader, poolIndex);
        }

        static string addGetMethods(ComponentInfo componentInfo, int poolIndex) {
            var getMethod = componentInfo.isSingletonComponent
                    ? "\n    static readonly $Type $nameComponent = new $Type();\n"
                    : "\n    public $Type $name { get { return ($Type)GetComponent($Ids.$Name); } }\n";

            return buildString(componentInfo, getMethod, poolIndex);
        }

        static string addHasMethods(ComponentInfo componentInfo, int poolIndex) {
            var hasMethod = componentInfo.isSingletonComponent ? @"
    public bool $prefix$Name {
        get { return HasComponent($Ids.$Name); }
        set {
            if(value != $prefix$Name) {
                if(value) {
                    AddComponent($Ids.$Name, $nameComponent);
                } else {
                    RemoveComponent($Ids.$Name);
                }
            }
        }
    }

    public $Tag $Prefix$Name(bool value) {
        $prefix$Name = value;
        return this;
    }
" : @"
    public bool has$Name { get { return HasComponent($Ids.$Name); } }
";
            return buildString(componentInfo, hasMethod, poolIndex);
        }

        static string addAddMethods(ComponentInfo componentInfo, int poolIndex) {
            return componentInfo.isSingletonComponent ? string.Empty : buildString(componentInfo, @"
    public $Tag Add$Name($typedArgs) {
        var component = CreateComponent<$Type>($Ids.$Name);
$assign
        AddComponent($Ids.$Name, component);
        return this;
    }
", poolIndex);
        }

        static string addReplaceMethods(ComponentInfo componentInfo, int poolIndex) {
            return componentInfo.isSingletonComponent ? string.Empty : buildString(componentInfo, @"
    public $Tag Replace$Name($typedArgs) {
        var component = CreateComponent<$Type>($Ids.$Name);
$assign
        ReplaceComponent($Ids.$Name, component);
        return this;
    }
", poolIndex);
        }

        static string addRemoveMethods(ComponentInfo componentInfo, int poolIndex) {
            return componentInfo.isSingletonComponent ? string.Empty : buildString(componentInfo, @"
    public $Tag Remove$Name() {
        RemoveComponent($Ids.$Name);
        return this;
    }
", poolIndex);
        }

        /*
         *
         * POOL METHODS
         *
         */

        static string addPoolMethods(ComponentInfo componentInfo) {
            return addPoolClassHeader(componentInfo)
                    + addPoolGetMethods(componentInfo)
                    + addPoolHasMethods(componentInfo)
                    + addPoolAddMethods(componentInfo)
                    + addPoolReplaceMethods(componentInfo)
                    + addPoolRemoveMethods(componentInfo)
                    + addCloseClass();
        }

        static string addPoolClassHeader(ComponentInfo componentInfo) {
            const string classHeader = "\npublic partial class $TagPool : Pool<$Tag> {\n";
            return buildString(componentInfo, classHeader);
        }

        static string addPoolGetMethods(ComponentInfo componentInfo) {
            var getMehod = componentInfo.isSingletonComponent ? @"
    public $Tag $nameEntity { get { return GetGroup($TagMatcher.$Name).GetSingleEntity(); } }
" : @"
    public $Tag $nameEntity { get { return GetGroup($TagMatcher.$Name).GetSingleEntity(); } }

    public $Type $name { get { return $nameEntity.$name; } }
";
            return buildString(componentInfo, getMehod);
        }

        static string addPoolHasMethods(ComponentInfo componentInfo) {
            var hasMethod = componentInfo.isSingletonComponent ? @"
    public bool $prefix$Name {
        get { return $nameEntity != null; }
        set {
            var entity = $nameEntity;
            if(value != (entity != null)) {
                if(value) {
                    CreateEntity().$prefix$Name = true;
                } else {
                    DestroyEntity(entity);
                }
            }
        }
    }
" : @"
    public bool has$Name { get { return $nameEntity != null; } }
";
            return buildString(componentInfo, hasMethod);
        }

        static object addPoolAddMethods(ComponentInfo componentInfo) {
            return componentInfo.isSingletonComponent ? string.Empty : buildString(componentInfo, @"
    public $Tag Set$Name($typedArgs) {
        if(has$Name) {
            throw new EntitasException(""Could not set $name!\n"" + this + "" already has an entity with $Type!"",
                ""You should check if the pool already has a $nameEntity before setting it or use pool.Replace$Name()."");
        }
        var entity = CreateEntity();
        entity.Add$Name($args);
        return entity;
    }
");
        }

        static string addPoolReplaceMethods(ComponentInfo componentInfo) {
            return componentInfo.isSingletonComponent ? string.Empty : buildString(componentInfo, @"
    public $Tag Replace$Name($typedArgs) {
        var entity = $nameEntity;
        if(entity == null) {
            entity = Set$Name($args);
        } else {
            entity.Replace$Name($args);
        }

        return entity;
    }
");
        }

        static string addPoolRemoveMethods(ComponentInfo componentInfo) {
            return componentInfo.isSingletonComponent ? string.Empty : buildString(componentInfo, @"
    public void Remove$Name() {
        DestroyEntity($nameEntity);
    }
");
        }

        /*
        *
        * MATCHER
        *
        */

        static string addMatchers(ComponentInfo componentInfo) {
            const string matcherFormat = @"
public partial class $TagMatcher {

    static IMatcher<$Tag> _matcher$Name;

    public static IMatcher<$Tag> $Name {
        get {
            if(_matcher$Name == null) {
                var matcher = (Matcher<$Tag>)Matcher<$Tag>.AllOf($Ids.$Name);
                matcher.componentNames = $Ids.componentNames;
                _matcher$Name = matcher;
            }

            return _matcher$Name;
        }
    }
}
";

            var poolIndex = 0;
            var matchers = componentInfo.pools.Aggregate(string.Empty, (acc, poolName) => {
                return acc + buildString(componentInfo, matcherFormat, poolIndex++);
            });

            return buildString(componentInfo, matchers);
        }

        /*
         *
         * HELPERS
         *
         */

        static string buildString(ComponentInfo componentInfo, string format, int poolIndex = 0) {
            format = createFormatString(format);
            var a0_type = componentInfo.fullTypeName;
            var a1_name = componentInfo.typeName.RemoveComponentSuffix();
            var a2_lowercaseName = a1_name.LowercaseFirst();
            var poolNames = componentInfo.pools;
            var a3_tag = poolNames[poolIndex];
            var lookupTags = componentInfo.ComponentLookupTags();
            var a4_ids = lookupTags[poolIndex];
            var memberInfos = componentInfo.memberInfos;
            var a5_memberNamesWithType = memberNamesWithType(memberInfos);
            var a6_memberAssigns = memberAssignments(memberInfos);
            var a7_memberNames = memberNames(memberInfos);
            var prefix = componentInfo.singleComponentPrefix;
            var a8_prefix = prefix.UppercaseFirst();
            var a9_lowercasePrefix = prefix.LowercaseFirst();

            return string.Format(format, a0_type, a1_name, a2_lowercaseName,
                a3_tag, a4_ids, a5_memberNamesWithType, a6_memberAssigns, a7_memberNames,
                a8_prefix, a9_lowercasePrefix);
        }

        static string createFormatString(string format) {
            return format.Replace("{", "{{")
                        .Replace("}", "}}")
                        .Replace("$Type", "{0}")
                        .Replace("$Name", "{1}")
                        .Replace("$name", "{2}")
                        .Replace("$Tag", "{3}")
                        .Replace("$Ids", "{4}")
                        .Replace("$typedArgs", "{5}")
                        .Replace("$assign", "{6}")
                        .Replace("$args", "{7}")
                        .Replace("$Prefix", "{8}")
                        .Replace("$prefix", "{9}");
        }

        static string memberNamesWithType(List<PublicMemberInfo> memberInfos) {
            var typedArgs = memberInfos
                .Select(info => info.type.ToCompilableString() + " new" + info.name.UppercaseFirst())
                .ToArray();

            return string.Join(", ", typedArgs);
        }

        static string memberAssignments(List<PublicMemberInfo> memberInfos) {
            const string format = "        component.{0} = {1};";
            var assignments = memberInfos.Select(info => {
                var newArg = "new" + info.name.UppercaseFirst();
                return string.Format(format, info.name, newArg);
            }).ToArray();

            return string.Join("\n", assignments);
        }

        static string memberNames(List<PublicMemberInfo> memberInfos) {
            var args = memberInfos.Select(info => "new" + info.name.UppercaseFirst()).ToArray();
            return string.Join(", ", args);
        }

        static string addCloseClass() {
            return "}\n";
        }
    }
}

