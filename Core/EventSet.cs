namespace CalamityFables.Core
{
    /// <summary>
    /// List of delegates indexed by an integer ID <br>
    /// Allows for behavior similar to regular events without having to loop through all different subscribed delegates and checking the type everytime
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EventSet<T> where T : Delegate
    {
        private Dictionary<int, T> invocationList = new();

        public void Add(int type, T call) => invocationList.Add(type, call);
        public T GetInvocation(int type)
        {
            if (invocationList.TryGetValue(type, out T del))
                return del;
            return null;
        }

        public bool TryGetInvocation(int type, out T del)
        {
            return invocationList.TryGetValue(type, out del);
        }
    }
}
