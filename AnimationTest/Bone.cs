using Assimp;
using GLOOP.Animation;
using GLOOP.Extensions;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AnimationTest
{
    public class Bone
    {
        public string Name { get; set; }
        public List<Bone> Children { get; private set; } = new List<Bone>();
        public TransformTimeline Timeline { get; private set; } = new TransformTimeline(true);
        public int[] BoundVerts { get; private set; }
        public float[] VertexWeights { get; private set; }
        public Matrix4 InitialTransform { get; internal set; }

        public Bone(string name)
        {
            Name = name;
        }

        public void AddAnimation(NodeAnimationChannel timeline, float ticksPerSecond)
        {
            // Assumes pos, scale and rot all have keyframes at the same time
            for (int i = 0; i < timeline.PositionKeyCount; i++)
            {
                Debug.Assert(
                    timeline.PositionKeys[i].Time == timeline.ScalingKeys[i].Time
                    && timeline.ScalingKeys[i].Time == timeline.RotationKeys[i].Time,
                    "Mismatching keyframes"
                );

                Timeline.AddKeyframe(
                    (float)(timeline.PositionKeys[i].Time * 1000f * ticksPerSecond),
                    new GLOOP.DynamicTransform(
                        timeline.PositionKeys[i].Value.ToOpenTK(),
                        timeline.ScalingKeys[i].Value.ToOpenTK(),
                        timeline.RotationKeys[i].Value.ToOpenTK()
                    )
                );

            }
        }

        public void AssignVertexWeights(List<VertexWeight> vertexWeights)
        {
            var sortedWeights = vertexWeights.OrderBy(w => w.Weight).Take(3);
            BoundVerts = sortedWeights.Select(w => w.VertexID).ToArray();
            var weights = sortedWeights.Select(w => w.Weight).ToArray();
            var v = new Vector3(weights[0], weights[1], weights[2]).Normalized();
            VertexWeights = new float[] { v.X, v.Y, v.Z };
        }

        public override string ToString() => Name;
    }
}
