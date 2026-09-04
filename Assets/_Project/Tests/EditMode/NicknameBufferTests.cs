using DeliveryBot.Delivery;
using DeliveryBot.UI;
using NUnit.Framework;

namespace DeliveryBot.Tests
{
    public class NicknameBufferTests
    {
        [Test]
        public void Append_RejectsControlChars()
        {
            var b = new NicknameBuffer();
            Assert.IsFalse(b.Append('\b'));
            Assert.IsFalse(b.Append('\n'));
            Assert.IsFalse(b.Append('\r'));
            Assert.IsTrue(b.Append('a'));
            Assert.IsTrue(b.Append(' '));
            Assert.IsTrue(b.Append('가'));
            Assert.AreEqual("a 가", b.Text);
        }

        [Test]
        public void Append_StopsAtMaxLength()
        {
            var b = new NicknameBuffer(3);
            Assert.IsTrue(b.Append('x'));
            Assert.IsTrue(b.Append('y'));
            Assert.IsTrue(b.Append('z'));
            Assert.IsFalse(b.Append('w'));
            Assert.AreEqual("xyz", b.Text);
        }

        [Test]
        public void Initial_IsCappedToo()
        {
            var b = new NicknameBuffer(4, "toolongname");
            Assert.AreEqual("tool", b.Text);
        }

        [Test]
        public void Backspace_OnEmptyReturnsFalse()
        {
            var b = new NicknameBuffer();
            Assert.IsFalse(b.Backspace());
            b.Append('a');
            Assert.IsTrue(b.Backspace());
            Assert.AreEqual("", b.Text);
        }

        [Test]
        public void Commit_Sanitizes()
        {
            var b = new NicknameBuffer();
            Assert.AreEqual(Leaderboard.DefaultName, b.Commit());
            b.Append(' '); b.Append('a'); b.Append(' ');
            Assert.AreEqual("a", b.Commit());
        }
    }
}
