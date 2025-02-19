﻿using Server;
using Server.Mobiles;
using System.Collections.Generic;

namespace Scripts.Mythik.Systems.Achievements
{
    public class AchievementSystemMemoryStone : Item
    {
        public static AchievementSystemMemoryStone GetInstance()
        {
            if (m_instance == null)
                m_instance = new AchievementSystemMemoryStone();
            m_instance.MoveToWorld(new Point3D(5627, 1138, 0), Map.Trammel);
            return m_instance;
        }
        internal Dictionary<Serial, Dictionary<int, AchieveData>> Achievements = new Dictionary<Serial, Dictionary<int, AchieveData>>();
        private Dictionary<Serial, int> m_PointsTotal = new Dictionary<Serial, int>();
        private static AchievementSystemMemoryStone m_instance;

        public int GetPlayerPointsTotal(PlayerMobile m)
        {
            if (!m_PointsTotal.ContainsKey(m.Serial))
                m_PointsTotal.Add(m.Serial, 0);
            return m_PointsTotal[m.Serial];
        }
        public void AddPoints(PlayerMobile m, int points)
        {
            if (!m_PointsTotal.ContainsKey(m.Serial))
                m_PointsTotal.Add(m.Serial, 0);
            m_PointsTotal[m.Serial] += points;
        }

        [Constructable]
        public AchievementSystemMemoryStone() : base(0xED4)
        {
            Visible = true;
            Name = "Achievement System Stone [~~DO NOT MOVE OR REMOVE~~]";
			Hue = 1085;
            m_instance = this;
        }

        public AchievementSystemMemoryStone(Serial serial) : base(serial)
        {
            m_instance = this;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version 

            writer.Write(m_PointsTotal.Count);
            foreach (var kv in m_PointsTotal)
            {
                writer.Write(kv.Key);
                writer.Write(kv.Value);
            }

            writer.Write(Achievements.Count);
            foreach (var kv in Achievements)
            {
                writer.Write(kv.Key);
                writer.Write(kv.Value.Count);
                foreach (var ac in kv.Value)
                {
                    writer.Write(ac.Key);
                    ac.Value.Serialize(writer);
                }
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            int count = reader.ReadInt();
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    m_PointsTotal.Add(reader.ReadInt(), reader.ReadInt());
                }
            }

            count = reader.ReadInt();
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    Serial id = reader.ReadInt();
                    var dict = new Dictionary<int, AchieveData>();
                    int iCount = reader.ReadInt();
                    if (iCount > 0)
                    {
                        for (int x = 0; x < iCount; x++)
                        {
                            dict.Add(reader.ReadInt(), new AchieveData(reader));
                        }

                    }
                    Achievements.Add(id, dict);
                }
            }
        }
    }
}