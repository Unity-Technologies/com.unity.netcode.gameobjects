
namespace MLAPI.Logging
{
    public static class ClientContext
    {
        private enum ContextKey
        {
            HiddenClientIDValue,
        }
    
        public static Context NewContext(Context parent, ulong clientID)
        {
            return Context.WithValue(parent, ContextKey.HiddenClientIDValue.ToString(), clientID);
        }
        
        public static string FromContext(Context ctx)
        {
            return ctx.GetProperty(ContextKey.HiddenClientIDValue.ToString()).ToString();
        }
    }
}
