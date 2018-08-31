using Microsoft.VisualStudio.Shell;
using System.ComponentModel.Design;

namespace NopyCopyV2.Modals
{
    /// <summary>
    ///     Most likely going to delete this.
    ///     Suppose to be a more dev-friendly but less powerfull version of
    ///     Package.
    /// </summary>
    public class PackageV2 : Package
    {
        protected void AddService<S, T>()
            where T : Package, S, new()
        {
            var serviceContainer = this as IServiceContainer;

            ServiceCreatorCallback callback =
                new ServiceCreatorCallback((container, serviceType) =>
                {
                    if (typeof(S) == serviceType)
                        return new T();
                    return null;
                });
            serviceContainer.AddService(typeof(T), callback);
        }

        protected T GetService<S, T>() where T : class
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return GetService(typeof(S)) as T;
        }
    }
}
