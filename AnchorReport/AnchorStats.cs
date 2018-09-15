using System;
using System.Collections.Generic;
using System.Numerics;

namespace AnchorReport
{
    public class AnchorStats
    {
        /// <summary>
        /// Maintains data for a single anchor.  We monitor movement by
        /// maintaining the size of a AABB for all the locations of the anchor
        /// </summary>
        private class AnchorStat
        {
            private readonly Vector3 lever = new Vector3(0.2f, 0.2f, 0.2f);

            float minX, maxX, minY, maxY, minZ, maxZ;
            bool firstSample = true;

            public void AddSample(Matrix4x4 transform)
            {
                var point = Vector3.Transform(this.lever, transform);

                if (this.firstSample)
                {
                    minX = maxX = point.X;
                    minY = maxY = point.Y;
                    minZ = maxZ = point.Z;
                    this.firstSample = false;
                }
                else
                {
                    minX = Math.Min(minX, point.X);
                    maxX = Math.Max(maxX, point.X);
                    minY = Math.Min(minY, point.Y);
                    maxY = Math.Max(maxY, point.Y);
                    minZ = Math.Min(minZ, point.Z);
                    maxZ = Math.Max(maxZ, point.Z);
                }
            }

            public float MovementVolume
            {
                get
                {
                    return (
                        (maxX - minX) *
                        (maxY - minY) *
                        (maxZ - minZ)
                        );
                }
            }
        }

        private Dictionary<Guid, AnchorStat> stats = new Dictionary<Guid, AnchorStat>();
        private HashSet<Guid> ignoredAnchors = new HashSet<Guid>();

        public void AddSample(AnchorCollection anchors)
        {
            Dictionary<Guid, int> anchorCounts = new Dictionary<Guid, int>();

            foreach (var anchor in anchors.Anchors)
            {
                // make sure we haven't seen this anchor yet in the colection.  We
                // ignore anchors that appear more than once in a collection.  Bug in system?
                int count = 1;
                if (anchorCounts.TryGetValue(anchor.Guid, out count))
                {
                    count = count + 1;
                    this.ignoredAnchors.Add(anchor.Guid);
                    this.stats.Remove(anchor.Guid); // stop tracking this anchor
                }
                anchorCounts[anchor.Guid] = count;

                // See if we are ignoring this anchor.  Checking count > 1 could work but
                // we don't know if an anchor may go and come back
                if (this.ignoredAnchors.Contains(anchor.Guid))
                {
                    continue;
                }

                AnchorStat stat;
                if (!this.stats.TryGetValue(anchor.Guid, out stat))
                {
                    stat = new AnchorStat();
                    this.stats[anchor.Guid] = stat;
                }
                stat.AddSample(anchor.Transform);
            }
        }

        public float TotalMovementVolume
        {
            get
            {
                float sum = 0.0f;
                foreach (var stat in this.stats.Values)
                {
                    sum += stat.MovementVolume;
                }
                return sum;
            }
        }

        public float MeanMovementVolume
        {
            get
            {
                return this.TotalMovementVolume / this.stats.Count;
            }
        }

        public float AnchorMovementVolume(Guid guid)
        {
            AnchorStat stat;

            if (this.stats.TryGetValue(guid, out stat))
            {
                return stat.MovementVolume;
            }
            return -1;
        }

        /// <summary>
        /// Returns the list of anchors we're ignoring because we've seen duplicates
        /// </summary>
        public Guid[] IgnoredAnchors
        {
            get
            {
                var retval = new Guid[this.ignoredAnchors.Count];
                int index = 0;
                foreach(var guid in this.ignoredAnchors)
                {
                    retval[index] = guid;
                    index++;
                }
                return retval;
            }
        }
    }
}