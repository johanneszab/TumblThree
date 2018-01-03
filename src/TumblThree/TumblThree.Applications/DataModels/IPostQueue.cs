using System.Collections.Generic;

namespace TumblThree.Applications.DataModels
{
    public interface IPostQueue<T>
    {
        void Add(T post);
        void CompleteAdding();
        IEnumerable<T> GetConsumingEnumerable();
    }
}