using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP
{
    public static class TaskMaster
    {
        private class Job
        {
            public Func<bool> Condition;
            public Action Action;

            public Job(Func<bool> condition, Action action)
            {
                this.Condition = condition;
                this.Action = action;
            }
        }

        private static readonly Queue<Job> Jobs = new Queue<Job>(10);

        public static void AddTask(Func<bool> condition, Action action)
        {
            Jobs.Enqueue(new Job(condition, action));
        }

        public static void Process()
        {
            if (Jobs.Count > 0 && Jobs.Peek().Condition())
            {
                Jobs.Dequeue().Action();
            }
        }
    }
}
