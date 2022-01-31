
namespace SugarGuard.Core
{
    public abstract class Protection
    {
        public abstract string Name { get; }
        public abstract void Execute(Context context);
    }
}
