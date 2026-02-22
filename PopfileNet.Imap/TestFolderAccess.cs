using MailKit;

namespace Test;

class FolderAccessor {
    public void Test() {
        // Correct usage: fully qualified name
        var access = MailKit.FolderAccess.ReadOnly;
        System.Console.WriteLine(access);
    }
}