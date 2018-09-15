using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace AnchorReport
{
    public class Anchor
    {
        public Anchor(Guid guid, Matrix4x4 transform)
        {
            this.Guid = guid;
            this.Transform = transform;
            this.ValidTransform = true;
        }

        public Anchor(Guid guid)
        {
            this.Guid = guid;
            this.Transform = Matrix4x4.Identity;
            this.ValidTransform = false;
        }

        public Guid Guid { get; private set; }

        public bool ValidTransform { get; private set;}

        public Matrix4x4 Transform { get; private set; }
    }

    /// <summary>
    /// Handles de-serialization of the anchors returned from the HoloLens
    /// </summary>
    public class AnchorCollection
    {
        private List<Anchor> anchors = new List<Anchor>();

        public IList<Anchor> Anchors { get { return this.anchors; } }

        public AnchorCollection(string json)
        {
            PortalAnchorCollection portalAnchors = new PortalAnchorCollection();

            var serializer = new DataContractJsonSerializer(typeof(PortalAnchorCollection));
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json ?? ""));

            try
            {
                portalAnchors = (PortalAnchorCollection)serializer.ReadObject(stream);
            }
            catch(Exception)
            {
                // probably shouldn't just eat exceptions :-)
            }

            foreach(var portalAnchor in portalAnchors.Anchors)
            {
                Anchor newAnchor;

                var guid = Guid.Parse(portalAnchor.GUID);
                if (portalAnchor.Transform != null)
                {
                    var transform = new Matrix4x4(
                        portalAnchor.Transform[0],
                        portalAnchor.Transform[1],
                        portalAnchor.Transform[2],
                        portalAnchor.Transform[3],
                        portalAnchor.Transform[4],
                        portalAnchor.Transform[5],
                        portalAnchor.Transform[6],
                        portalAnchor.Transform[7],
                        portalAnchor.Transform[8],
                        portalAnchor.Transform[9],
                        portalAnchor.Transform[10],
                        portalAnchor.Transform[11],
                        portalAnchor.Transform[12],
                        portalAnchor.Transform[13],
                        portalAnchor.Transform[14],
                        portalAnchor.Transform[15]
                        );
                    newAnchor = new Anchor(guid, transform);
                }
                else
                {
                    newAnchor = new Anchor(guid);
                }

                this.anchors.Add(newAnchor);
            }
        }


        [DataContract]
        class PortalAnchorCollection
        {
            [DataMember]
            public PortalAnchor[] Anchors { get; private set; } = new PortalAnchor[0];
        }

        [DataContract]
        class PortalAnchor
        {
            [DataMember]
            public string GUID { get; private set; }

            [DataMember]
            public float[] Transform { get; private set; }
        }
    }
}
