using System.Collections;
using System.Collections.Generic;

namespace Pandora.BehaviorTree
{
    /// <summary>
    /// 延时删除元素的无序容器
    /// 使用场景：需要在遍历集合过程中要删除集合中的元素.
    /// 缺点：在遍历时大量删除元素会导致性能下降
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DelayRemoveContainer<T> : IEnumerable<T>
    {
        private List<T> datas;
        /// <summary>
        /// 是否在遍历集合中
        /// </summary>
        public Accumulator eachOperating = new ();
        private int count = 0;
        public int Count => count;
        
        /// <summary>
        /// 待删除下标的列表
        /// 不能直接存储对象因为容器允许存储重复对象,比如:可以存储多个相同的数字.
        /// </summary>
        private List<int> pendingRemoved = new();

        public DelayRemoveContainer(int capacity = 0)
        {
            datas = new(capacity);
        }

        /// <summary>
        /// 新增元素
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            datas.Add(item);
            ++count;
        }
        
        /// <summary>
        /// 标记删除对象
        /// </summary>
        /// <param name="item"></param>
        public void MarkRemove(T item)
        {
            for (var i = datas.Count - 1 ; i >= 0; --i)
            {
                if (!datas[i].Equals(item)) continue;
                
                if (!pendingRemoved.Contains(i))
                {
                    --count;
                    if (eachOperating)
                    {
                        pendingRemoved.Add(i);
                    }
                    else
                    {
                        datas.RemoveAt(i);
                    }
                    break;
                }
            }
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// 清除全部数据
        /// </summary>
        public void Clear()
        {
            count = 0;
            if (!eachOperating)
            {
                pendingRemoved.Clear();
                datas.Clear();
                return;
            }

            if (datas.Count > 0)
            {
                List<int> appendRemovalList = new(datas.Count);
                for (var i = datas.Count - 1; i >= 0; --i)
                {
                    if (!pendingRemoved.Contains(i))
                    {
                        appendRemovalList.Add(i);
                    }
                }

                foreach (var newIdx in appendRemovalList)
                {
                    pendingRemoved.Add(newIdx);
                }
            }
        }
 
        /// <summary>
        /// 执行清理
        /// </summary>
        private void Cleanup()
        {
            if (eachOperating) return;
            if (pendingRemoved.Count <= 0) return;
            pendingRemoved.Sort((x, y) =>
            {
                if (x > y) return -1;
                return x == y ? 0 : 1;
            });

            foreach (var idx in pendingRemoved)
            {
                for (int i = datas.Count - 1; i >= 0; --i)
                {
                    if (i == idx)
                    {
                        datas.RemoveAt(i);
                        break;
                    }
                }
            }

            pendingRemoved.Clear();
            count = datas.Count;
        }
        
        
        private class Enumerator : IEnumerator<T>
        {
            private T current;
            object IEnumerator.Current => current;
            private DelayRemoveContainer<T> container;
            private int index = 0;

            public Enumerator(DelayRemoveContainer<T> c)
            {
                container = c;
                container.eachOperating++;
                index = 0;
            }
            
            public bool MoveNext()
            {
                while (index < container.datas.Count)
                {
                    if (!container.pendingRemoved.Contains(index))
                    {
                        current = container.datas[index];
                        ++index;
                        return true;
                    }
                    ++index;
                }

                return false;
            }

            public void Reset()
            {
                index = 0;
                current = default;
            }

            public T Current => current;

            public void Dispose()
            {
                container.eachOperating--;
                container.Cleanup();
            }
        }

        
    }
    
}