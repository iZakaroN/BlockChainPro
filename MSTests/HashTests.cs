using System;
using BlockChanPro.Core.Contracts;
using BlockChanPro.Core.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BlockChanPro.MSTESTS
{
    [TestClass]
    public class HashTests
    {
        [TestMethod]
        public void Hash_Increase_0x01_Validate_Transfer_LowSegment()
        {
            var hash = new Hash(new byte[]
                {
                    0,0,0,0,0,0,0,0,
                    0,0,0,0,0,0,0,0,
                    0,0,0,0,0,0,0,0,
                    0,0,0,0,0,0,0,0xff,
                });

            hash.Increment(1);

            for (int i = 0; i < Hash.SegmentsLength - 2; i++)
                Assert.AreEqual(0, hash.Value[i]);
            Assert.AreEqual(0x01, hash.Value[Hash.SegmentsLength - 2]);
            Assert.AreEqual(0x00, hash.Value[Hash.SegmentsLength - 1]);
        }

        [TestMethod]
        public void Hash_Increase_0xff_Validate_Transfer_LowSegment()
        {
            var hash = new Hash(new byte[]
                {
                    0,0,0,0,0,0,0,0,
                    0,0,0,0,0,0,0,0,
                    0,0,0,0,0,0,0,0,
                    0,0,0,0,0,0,0,0xff,
                });

            hash.Increment(0xff);

            for (int i = 0; i < Hash.SegmentsLength - 2; i++)
                Assert.AreEqual(0, hash.Value[i]);
            Assert.AreEqual(0x01, hash.Value[Hash.SegmentsLength - 2]);
            Assert.AreEqual(0xfe, hash.Value[Hash.SegmentsLength - 1]);
        }

        [TestMethod]
        public void Hash_Increase_0xff_Validate_Transfer_HighSegment()
        {
            var hash = new Hash(new byte[]
                {
                    0xfe,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
                    0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
                    0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
                    0xff,0xff,0xff,0xff,0xff,0xff,0xff,0x01,
                });

            hash.Increment(0xff);

            for (int i = 1; i < Hash.SegmentsLength; i++)
                Assert.AreEqual(0x00, hash.Value[i]);
            Assert.AreEqual(0xff, hash.Value[0]);
        }

        [TestMethod]
        [ExpectedException(typeof(OverflowException))]
        public void Hash_Increase_0xff_Validate_Transfer_HighSegment_OverflowException()
        {
            var hash = new Hash(new byte[]
                {
                    0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
                    0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
                    0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
                    0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
                });

            hash.Increment(0xff);

            for (int i = 0; i < Hash.SegmentsLength; i++)
                Assert.AreEqual(0xff, hash.Value[i]);
        }

        [TestMethod]
        [ExpectedException(typeof(OverflowException))]
        public void Hash_Increase_LongMax_Validate_Transfer_LowSegment_OverflowException()
        {
            var hash = new Hash(new byte[]
                {
                    0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
                    0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
                    0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
                    0xff,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
                });

            hash.Increment(long.MaxValue);

            for (int i = 0; i < Hash.SegmentsLength; i++)
                Assert.AreEqual(0xff, hash.Value[i]);
        }

        [TestMethod]
        public void Hash_Increase_LongMax_Validate_LongMax_Segments()
        {
            var hash = new Hash(new byte[]
                {
                    0,0,0,0,0,0,0,0,
                    0,0,0,0,0,0,0,0,
                    0,0,0,0,0,0,0,0,
                    0,0,0,0,0,0,0,0,
                });

            hash.Increment(long.MaxValue);
            var meaningSegments = sizeof(long) * 8 / Hash.SegmentBitSize;
            var calculatedHashValue = 0L;
            var segmentPower = 1L;
            for (int i = 0; i < meaningSegments; i++)
            {
                calculatedHashValue += hash.Value[Hash.SegmentsLength - 1 - i] * segmentPower;
                segmentPower <<= Hash.SegmentBitSize;
            }
            Assert.AreEqual(long.MaxValue, calculatedHashValue);
        }

        [TestMethod]
        public void Hash_Compare_Validate_Equal()
        {
            var hash = new Hash(new byte[]
                {
                    17,0,0,0,0,0xff,0,0,
                    0,0,0,1,0,0,0,0,
                    0,0x55,0,0,2,0,0,0,
                    0,0,4,0,0,5,0,0,
                });

            Assert.AreEqual(0, hash.Compare(hash));
        }

        [TestMethod]
        public void Hash_Compare_Validate_Less()
        {
            var left = new Hash(new byte[]
                {
                    0xff,0,0,0,0,0,0,0xff,
                    0x11,0,0,0,0,0,0,0,
                    0,0,0,0,0,0,0,0,
                    0,0,0,0,0,0,0,0,
                });

            var right = new Hash(new byte[]
                {
                    0xff,0,0,0,0,0,0,0xff,
                    0x22,0,0,0,0,0,0,0,
                    0,0,0,0,0,0,0,0,
                    0,0,0,0,0,0,0,0,
                });

            Assert.AreEqual(-1, left.Compare(right));
        }

        [TestMethod]
        public void Hash_Compare_Validate_More()
        {
            var right = new Hash(new byte[]
                {
                    0xff,0,0,0,0,0,0,0xff,
                    0x11,0,0,0,0,0,0,0,
                    0,0,0,0,0,0,0,0,
                    0,0,0,0,0,0,0,0,
                });

            var left = new Hash(new byte[]
                {
                    0xff,0,0,0,0,0,0,0xff,
                    0x22,0,0,0,0,0,0,0,
                    0,0,0,0,0,0,0,0,
                    0,0,0,0,0,0,0,0,
                });

            Assert.AreEqual(1, left.Compare(right));
        }

        [TestMethod]
        public void TargetHashBits_ToHash_Offset0_Validate_Segments()
        {
            var thb = new HashBits(0, 0x123456789abcde);

            var hash = thb.ToHash();

            Assert.AreEqual(0x12, hash.Value[0]);
            Assert.AreEqual(0x34, hash.Value[1]);
            Assert.AreEqual(0x56, hash.Value[2]);
            Assert.AreEqual(0x78, hash.Value[3]);
            Assert.AreEqual(0x9a, hash.Value[4]);
            Assert.AreEqual(0xbc, hash.Value[5]);
            Assert.AreEqual(0xde, hash.Value[6]);
            Assert.AreEqual(0x00, hash.Value[7]);
        }

        [TestMethod]
        public void TargetHashBits_ToHash_Offset1_Validate_Segments()
        {
            var thb = new HashBits(1, 0xffffffffffffff);

            var hash = thb.ToHash();

            Assert.AreEqual(0x7f, hash.Value[0]);
            Assert.AreEqual(0xff, hash.Value[1]);
            Assert.AreEqual(0xff, hash.Value[2]);
            Assert.AreEqual(0xff, hash.Value[3]);
            Assert.AreEqual(0xff, hash.Value[4]);
            Assert.AreEqual(0xff, hash.Value[5]);
            Assert.AreEqual(0xff, hash.Value[6]);
            Assert.AreEqual(0x80, hash.Value[7]);
            Assert.AreEqual(0x00, hash.Value[8]);
        }

        [TestMethod]
        public void TargetHashBits_ToHash_Offset4_Validate_Segments()
        {
            var thb = new HashBits(4, 0xffffffffffffff);

            var hash = thb.ToHash();

            Assert.AreEqual(0x0f, hash.Value[0]);
            Assert.AreEqual(0xff, hash.Value[1]);
            Assert.AreEqual(0xff, hash.Value[2]);
            Assert.AreEqual(0xff, hash.Value[3]);
            Assert.AreEqual(0xff, hash.Value[4]);
            Assert.AreEqual(0xff, hash.Value[5]);
            Assert.AreEqual(0xff, hash.Value[6]);
            Assert.AreEqual(0xf0, hash.Value[7]);
            Assert.AreEqual(0x00, hash.Value[8]);
        }

        [TestMethod]
        public void TargetHashBits_ToHash_Offset8_Validate_Segments()
        {
            var thb = new HashBits(8, 0x123456789abcde);

            var hash = thb.ToHash();

            Assert.AreEqual(0x00, hash.Value[0]);
            Assert.AreEqual(0x12, hash.Value[1]);
            Assert.AreEqual(0x34, hash.Value[2]);
            Assert.AreEqual(0x56, hash.Value[3]);
            Assert.AreEqual(0x78, hash.Value[4]);
            Assert.AreEqual(0x9a, hash.Value[5]);
            Assert.AreEqual(0xbc, hash.Value[6]);
            Assert.AreEqual(0xde, hash.Value[7]);
            Assert.AreEqual(0x00, hash.Value[8]);
        }

        [TestMethod]
        public void TargetHashBits_ToHash_Offset193_Validate_Segments()
        {
            var thb = new HashBits(193, 0xffffffffffffff);

            var hash = thb.ToHash();

            Assert.AreEqual(0x00, hash.Value[23]);
            Assert.AreEqual(0x7f, hash.Value[24]);
            Assert.AreEqual(0xff, hash.Value[25]);
            Assert.AreEqual(0xff, hash.Value[26]);
            Assert.AreEqual(0xff, hash.Value[27]);
            Assert.AreEqual(0xff, hash.Value[28]);
            Assert.AreEqual(0xff, hash.Value[29]);
            Assert.AreEqual(0xff, hash.Value[30]);
            Assert.AreEqual(0x80, hash.Value[31]);
        }

        [TestMethod]
        public void TargetHashBits_ToHash_Offset196_Validate_Segments()
        {
            var thb = new HashBits(196, 0xffffffffffffff);

            var hash = thb.ToHash();


            Assert.AreEqual(0x00, hash.Value[23]);
            Assert.AreEqual(0x0f, hash.Value[24]);
            Assert.AreEqual(0xff, hash.Value[25]);
            Assert.AreEqual(0xff, hash.Value[26]);
            Assert.AreEqual(0xff, hash.Value[27]);
            Assert.AreEqual(0xff, hash.Value[28]);
            Assert.AreEqual(0xff, hash.Value[29]);
            Assert.AreEqual(0xff, hash.Value[30]);
            Assert.AreEqual(0xf0, hash.Value[31]);
        }

        [TestMethod]
        public void TargetHashBits_ToHash_Offset200_Validate_Segments()
        {
            var thb = new HashBits(200, 0x123456789abcde);

            var hash = thb.ToHash();

            Assert.AreEqual(0x00, hash.Value[24]);
            Assert.AreEqual(0x12, hash.Value[25]);
            Assert.AreEqual(0x34, hash.Value[26]);
            Assert.AreEqual(0x56, hash.Value[27]);
            Assert.AreEqual(0x78, hash.Value[28]);
            Assert.AreEqual(0x9a, hash.Value[29]);
            Assert.AreEqual(0xbc, hash.Value[30]);
            Assert.AreEqual(0xde, hash.Value[31]);

        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TargetHashBits_ToHash_Offset201_Validate_ArgumentException()
        {
            var thb = new HashBits(201, 0xffffffffffffff);

            var hash = thb.ToHash();

            //Checked only if TargetHashBits has no limit on offset
            Assert.AreEqual(0x00, hash.Value[24]);
            Assert.AreEqual(0x7f, hash.Value[25]);
            Assert.AreEqual(0xff, hash.Value[26]);
            Assert.AreEqual(0xff, hash.Value[27]);
            Assert.AreEqual(0xff, hash.Value[28]);
            Assert.AreEqual(0xff, hash.Value[29]);
            Assert.AreEqual(0xff, hash.Value[30]);
            Assert.AreEqual(0xff, hash.Value[31]);
            //Assert.AreEqual(0x80, hash.Value[32]);//We loose this bits
        }

        [TestMethod]
        public void TargetHashBits_Adjust_SixTime_Validate_OffsetAndFraction()
        {
            var thb = new HashBits(6, 0xffffffffffffff);
            var adjustedthb = thb.Adjust(6, 1);

            Assert.AreEqual(3, adjustedthb.GetBitOffset());
            Assert.IsTrue(thb.GetFraction() > adjustedthb.GetFraction());
        }

        [TestMethod]
        public void TargetHashBits_Adjust_QuadTime_Validate_OffsetAndFraction()
        {
            var thb = new HashBits(6, 0xffffffffffffff);
            var adjustedthb = thb.Adjust(4, 1);

            Assert.AreEqual(4, adjustedthb.GetBitOffset());
            Assert.AreEqual(thb.GetFraction(), adjustedthb.GetFraction());
        }

        [TestMethod]
        public void TargetHashBits_Adjust_TripleTime_Validate_Offset()
        {
            var thb = new HashBits(6, 0xffffffffffffff);
            var adjustedthb = thb.Adjust(3, 1);

            Assert.AreEqual(4, adjustedthb.GetBitOffset());
            Assert.IsTrue(thb.GetFraction() > adjustedthb.GetFraction());
        }

        [TestMethod]
        public void TargetHashBits_Adjust_DoublePlusTime_Validate_OffsetAndFraction()
        {
            var thb = new HashBits(2, 0xffffffffffffff);
            var adjustedthb = thb.Adjust(2001, 1000);

            Assert.AreEqual(0, adjustedthb.GetBitOffset());
            Assert.IsTrue(thb.GetFraction() > adjustedthb.GetFraction());
        }

        [TestMethod]
        public void TargetHashBits_Adjust_DoubleTime_Validate_OffsetAndFraction()
        {
            var thb = new HashBits(2, 0xffffffffffffff);
            var adjustedthb = thb.Adjust(2, 1);

            Assert.AreEqual(1, adjustedthb.GetBitOffset());
            Assert.AreEqual(thb.GetFraction(), adjustedthb.GetFraction());
        }

        [TestMethod]
        public void TargetHashBits_Adjust_DoubleMinusTime_HalfFraction_Validate_OffsetAndFraction()
        {
            var thb = new HashBits(2, 0x88888888888888);
            var adjustedthb = thb.Adjust(1999, 1000);

            Assert.AreEqual(1, adjustedthb.GetBitOffset());
            Assert.IsTrue(thb.GetFraction() > adjustedthb.GetFraction());
        }

        [TestMethod]
        public void TargetHashBits_Adjust_DoubleMinusTime_FullFraction_Validate_OffsetAndFraction()
        {
            var thb = new HashBits(2, 0xffffffffffffff);
            var adjustedthb = thb.Adjust(1999, 1000);

            Assert.AreEqual(1, adjustedthb.GetBitOffset());
            Assert.IsTrue(thb.GetFraction() > adjustedthb.GetFraction());
        }

        [TestMethod]
        public void TargetHashBits_Adjust_ThirthHalfTime_Validate_Offset()
        {
            var thb = new HashBits(5, 0xffffffffffffff);
            var adjustedthb = thb.Adjust(3000, 2000);

            Assert.AreEqual(4, adjustedthb.GetBitOffset());
            Assert.IsTrue(thb.GetFraction() > adjustedthb.GetFraction());
        }

        [TestMethod]
        public void TargetHashBits_Adjust_SameTime_Validate_OffsetAndFraction()
        {
            var thb = new HashBits(6, 0xffffffffffffff);
            var adjustedthb = thb.Adjust(1, 1);

            Assert.AreEqual(6, adjustedthb.GetBitOffset());
            Assert.AreEqual(thb.GetFraction(), adjustedthb.GetFraction());
        }

        [TestMethod]
        public void TargetHashBits_Adjust_TwoThirthTime_Validate_Offset()
        {
            var thb = new HashBits(6, 0xffffffffffffff);
            var adjustedthb = thb.Adjust(2, 3);

            Assert.AreEqual(6, adjustedthb.GetBitOffset());
            Assert.IsTrue(thb.GetFraction() > adjustedthb.GetFraction());
        }

        [TestMethod]
        public void TargetHashBits_Adjust_HalfPlusTime_Validate_OffsetAndFraction()
        {
            var thb = new HashBits(6, 0xffffffffffffff);
            var adjustedthb = thb.Adjust(201, 100);

            Assert.AreEqual(4, adjustedthb.GetBitOffset());
            Assert.IsTrue(thb.GetFraction() > adjustedthb.GetFraction());
        }

        [TestMethod]
        public void TargetHashBits_Adjust_HalfTime_Validate_OffsetAndFraction()
        {
            var thb = new HashBits(6, 0xffffffffffffff);
            var adjustedthb = thb.Adjust(100, 200);
            var hash = adjustedthb.ToHash().SerializeToJson();
            Console.WriteLine($"o({adjustedthb.GetBitOffset()}),\tf(0x{adjustedthb.GetFraction():X16}),\th({hash})");

            Assert.AreEqual(7, adjustedthb.GetBitOffset());
            Assert.AreEqual(thb.GetFraction(), adjustedthb.GetFraction());
        }

        [TestMethod]
        public void TargetHashBits_Adjust_HalfMinusTime_Validate_OffsetAndFraction()
        {
            var thb = new HashBits(6, 0xffffffffffffff);
            var adjustedthb = thb.Adjust(99, 200);
            var hash = adjustedthb.ToHash().SerializeToJson();
            Console.WriteLine($"o({adjustedthb.GetBitOffset()}),\tf(0x{adjustedthb.GetFraction():X16}),\th({hash})");

            Assert.AreEqual(7, adjustedthb.GetBitOffset());
            Assert.IsTrue(thb.GetFraction() > adjustedthb.GetFraction());
        }

        [TestMethod]
        public void TargetHashBits_Adjust_OneThirthTime_Validate_Offset()
        {
            var thb = new HashBits(6, 0xffffffffffffff);
            var adjustedthb = thb.Adjust(1, 3);

            Assert.AreEqual(7, adjustedthb.GetBitOffset());
            Assert.IsTrue(thb.GetFraction() > adjustedthb.GetFraction());
        }

        [TestMethod]
        public void TargetHashBits_Adjust_OneForthTime_Validate_OffsetAndFraction()
        {
            var thb = new HashBits(6, 0xffffffffffffff);
            var adjustedthb = thb.Adjust(1, 4);

            Assert.AreEqual(8, adjustedthb.GetBitOffset());
            Assert.AreEqual(thb.GetFraction(), adjustedthb.GetFraction());
        }

        [TestMethod]
        public void TargetHashBits_Adjust_OneSixthTime_Validate_OffsetAndFraction()
        {
            var thb = new HashBits(6, 0xffffffffffffff);
            var adjustedthb = thb.Adjust(1, 6);

            Assert.AreEqual(8, adjustedthb.GetBitOffset());
            Assert.IsTrue(thb.GetFraction() > adjustedthb.GetFraction());
        }

        [TestMethod]
        public void TargetHashBits_Adjust__Smoke_ConsoleOutput()
        {
            var target = 10;
            var thb = new HashBits(100, 0xffffffffffffff);
            //var thb = new TargetHashBits(100, 0x88888888888888);

            for (int i = 1; i <= 40; i++)
            {
                var adjustedthb = thb.Adjust(i, target);
                var hash = adjustedthb.ToHash().SerializeToJson();
                Console.WriteLine($"{i}/{target}:\to({adjustedthb.GetBitOffset()}),\tf(0x{adjustedthb.GetFraction():X16}),\th({hash})");
            }
        }

    }
}
