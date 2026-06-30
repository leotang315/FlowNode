using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FlowNode.Tests.Editor
{
    public static class TestRunner
    {
        public static int Main()
        {
            try { Console.OutputEncoding = Encoding.UTF8; } catch { }

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
                        var firstFrame = (inner.StackTrace ?? "").Split('\n');
                        if (firstFrame.Length > 0)
                            Console.WriteLine("        at " + firstFrame[0].Trim());
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
