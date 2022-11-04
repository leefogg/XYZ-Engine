using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GLOOP.Animation
{
    public class SkeletonAnimationSet : List<SkeletonAnimation>
    {
        public SkeletonAnimationSet(int count) : base(count) { }
        public SkeletonAnimationSet(IList<SkeletonAnimation> animations) : base(animations) { }

        public void MergeAllAs(string newName)
        {
            if (Count == 1)
                return;

            var combinedAnim = new SkeletonAnimation(
                this.SelectMany(anim => anim.Bones).ToArray(),
                newName
            );
            Clear();
            Add(combinedAnim);
        }
    }
}
