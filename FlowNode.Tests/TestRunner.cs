using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FlowNode.Tests
{
    /// <summary>
    /// 轻量测试 runner：反射扫描本程序集中带 [TestFixture] 的类与 [Test] 方法并执行，
    /// 失败时以非零退出码返回，便于 CI 集成。仅依赖 NUnit 的特性与断言，不依赖其引擎。
    /// </summary>
    public static class TestRunner
    {
        public static int Main()
        {
            try { Console.OutputEncoding = Encoding.UTF8; } catch { /* 控制台不支持时忽略 */ }

            int passed = 0, failed = 0;
            var asm = typeof(TestRunner).Assembly;

            var fixtures = asm.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && HasAttr(t.GetCustomAttributes(), "TestFixtureAttribute"))
                .OrderBy(t => t.Name);

            foreach (var fixture in fixtures)
            {
                var methods = fixture.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.GetParameters().Length == 0 && HasAttr(m.GetCustomAttributes(), "TestAttribute"))
                    .OrderBy(m => m.Name);

                foreach (var m in methods)
                {
                    try
                    {
                        var instance = Activator.CreateInstance(fixture);
                        m.Invoke(instance, null);
                        passed++;
                        Console.WriteLine($"  PASS  {fixture.Name}.{m.Name}");
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        var inner = (ex is TargetInvocationException tie && tie.InnerException != null)
                            ? tie.InnerException : ex;
                        Console.WriteLine($"  FAIL  {fixture.Name}.{m.Name}: {inner.GetType().Name}: {inner.Message}");
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Total: {passed + failed}, Passed: {passed}, Failed: {failed}");
            return failed == 0 ? 0 : 1;
        }

        private static bool HasAttr(System.Collections.Generic.IEnumerable<object> attrs, string attrTypeName)
        {
            return attrs.Any(a => a.GetType().Name == attrTypeName);
        }
    }
}
