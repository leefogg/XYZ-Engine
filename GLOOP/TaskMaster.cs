using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace GLOOP
{
    public static class TaskMaster
    {
        private class Job
        {
            public Func<bool> Condition;
            public Action Action;
            public string Name;
            public DateTime StartTime = DateTime.Now;

            public Job(Func<bool> condition, Action action, string name)
            {
                this.Condition = condition;
                this.Action = action;
                this.Name = name;
            }
        }

        private static readonly Queue<Job> Jobs = new Queue<Job>(10);
        private static int CompletedPerFrame = 0;

        public static void AddTask(Func<bool> condition, Action action, string name)
        {
            Jobs.Enqueue(new Job(condition, action, name));
        }

        public static void Process()
        {
            var completed = 0;
            if (Jobs.Count > 0 && Jobs.Peek().Condition())
            {
                Jobs.Dequeue().Action();
                completed++;
            }
            CompletedPerFrame = completed;
        }

        [Conditional("DEBUG")]
        public static void DrawImGuiWindow()
        {
            if (!ImGui.Begin("Task Master"))
                return;

            ImGui.Text($"Running Jobs ({Jobs.Count}):");
            foreach (var job in Jobs)
                ImGui.BulletText($"{job.Name} ({(DateTime.Now - job.StartTime).TotalMilliseconds.ToString("00.00")} ms)");

            ImGui.Text($"Completed per frame: {CompletedPerFrame}");
        }
    }
}
