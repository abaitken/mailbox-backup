using MailKit;
using NSubstitute;

namespace MailboxBackup.Tests;

[TestClass]
public class RemoteFolderViewTests
{
    [TestMethod]
    public void CanCombineRemotePath()
    {
        Assert.AreEqual("L1", RemoteFolderView.Combine("L1"));
        Assert.AreEqual("L1", RemoteFolderView.Combine("", "L1"));
        Assert.AreEqual("L1/L2", RemoteFolderView.Combine("L1", "L2"));
        Assert.AreEqual("L1/L2/L3", RemoteFolderView.Combine("L1", "L2", "L3"));
    }

    [TestMethod]
    public void CanCreateFolder1()
    {
        var topLevel = Substitute.For<IMailFolder>();
        topLevel.FullName.Returns("");

        var l1 = Substitute.For<IMailFolder>();
        l1.FullName.Returns("L1");
        var l2 = Substitute.For<IMailFolder>();
        l2.FullName.Returns("L1/L2");
        var l3 = Substitute.For<IMailFolder>();
        l3.FullName.Returns("L1/L2/L3");

        l2.Create("L3", true).Returns(l3);

        var subject = new RemoteFolderView(topLevel, new[] {l1, l2}.ToList(), new[] {l1, l2}.ToList());
        var actual = subject.Create("L1/L2/L3");
        Assert.AreSame(l3, actual);
    }

    [TestMethod]
    public void CanCreateFolder2()
    {
        var topLevel = Substitute.For<IMailFolder>();
        topLevel.FullName.Returns("");

        var l1 = Substitute.For<IMailFolder>();
        l1.FullName.Returns("L1");
        var l2 = Substitute.For<IMailFolder>();
        l2.FullName.Returns("L1/L2");
        var l3 = Substitute.For<IMailFolder>();
        l3.FullName.Returns("L1/L2/L3");
        var l4 = Substitute.For<IMailFolder>();
        l4.FullName.Returns("L1/L2/L3");

        l2.Create("L3", true).Returns(l3);
        l3.Create("L4", true).Returns(l4);

        var subject = new RemoteFolderView(topLevel, new[] {l1, l2}.ToList(), new[] {l1, l2}.ToList());
        var actual = subject.Create("L1/L2/L3/L4");
        Assert.AreSame(l4, actual);
    }

    [TestMethod]
    public void CanConvertRemotePathToLocalPath()
    {
        Assert.AreEqual("L1" + Path.DirectorySeparatorChar + "L2" + Path.DirectorySeparatorChar + "L3", RemoteFolderView.ConvertToFileSystemPath("L1/L2/L3"));
    }
}
