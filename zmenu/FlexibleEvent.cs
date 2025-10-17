using System;
using System.Reflection;
namespace SlideMenu
{
    public class FlexibleMethodDefinition
    {
        // This is a mostly AI generated class.
        // This is a much more easy to use version of action that handles return types, arbitrary argument types and ammounts.

        public Delegate method;
        public object[] args;

        public FlexibleMethodDefinition(Delegate method)
        {
            this.method = method;
            this.args = Array.Empty<object>();
        }
        public FlexibleMethodDefinition(Delegate method, object[] args)
        {
            this.method = method;
            this.args = args;
        }
        public FlexibleMethodDefinition(Action method)
        {
            this.method = method;
            this.args = Array.Empty<object>();
        }
        public FlexibleMethodDefinition(Func<sMenu> method)
        {
            this.method = method;
            this.args = Array.Empty<object>();
        }
        public static implicit operator FlexibleMethodDefinition(Delegate d)
        {
            return new FlexibleMethodDefinition(d);
        }
        public object? Invoke()
        {
            return Invoke(args);
        }
        public object? Invoke(params object[] suppliedArgs)
        {
            if (method == null)
                throw new InvalidOperationException("No method assigned to FlexibleMethodDefinition.");

            var finalArgs = suppliedArgs?.Length > 0 ? suppliedArgs : args;

            try
            {
                return method.DynamicInvoke(finalArgs);
            }
            catch (TargetParameterCountException e)
            {
                throw new InvalidOperationException(
                    $"Parameter count mismatch. Expected {method.Method.GetParameters().Length}, got {finalArgs?.Length ?? 0}.", e);
            }
            catch (Exception e)
            {
                throw new TargetInvocationException($"Error invoking method '{method.Method.Name}'.", e);
            }
        }
    }
    public class FlexibleEvent
    {
        private readonly OrderedSet<Action> listeners = new();
        public void Listen(Action method)
        {
            if (method == null) return;
            listeners.Add(method);
        }
        public void Listen(FlexibleMethodDefinition method)
        {
            Listen(method.method, method.args, method);
        }
        public void Listen(Delegate method, object[] args, FlexibleMethodDefinition fMethod = null)
        {
            if (method == null) return;

            var parameters = method.Method.GetParameters();
            object[] finalArgs = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                if (i < args.Length && args[i] != Default.Value)
                    finalArgs[i] = args[i];
                else if (parameters[i].IsOptional)
                    finalArgs[i] = parameters[i].DefaultValue;
                else
                    throw new ArgumentException($"Missing required argument '{parameters[i].Name}'");
            }

            void Wrapper()
            {
                method.DynamicInvoke(finalArgs);

                // Copy back ref/out changes to original args array
                if (fMethod?.args != null)
                {
                    for (int i = 0; i < finalArgs.Length && i < fMethod.args.Length; i++)
                        fMethod.args[i] = finalArgs[i];
                }
            }
            listeners.Add(Wrapper);
        }
        public void Unlisten(Action method)
        {
            listeners.Remove(method);
        }
        public void Unlisten(FlexibleMethodDefinition method)
        {
            // Note: This only works if the same wrapper instance is stored.
            // You may need to track the wrapper separately to remove it correctly.
            listeners.Remove((Action)method.method);
        }
        public void ClearListeners()
        {
            listeners.Clear();
        }
        public void Invoke()
        {
            foreach (var listener in listeners)
            {
                listener.Invoke();
            }
        }
    }
}