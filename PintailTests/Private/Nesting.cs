namespace Nanoray.Pintail.Tests.Private
{
    public static class Nesting
    {
        public sealed class Provider
        {
            public string Text
                => "lorem ipsum";

            public PlusOne Inner { get; } = new();

            public sealed class PlusOne
            {
                public string Text
                    => "lorem ipsum";

                public PlusTwo Inner { get; } = new();

                public sealed class PlusTwo
                {
                    public string Text
                        => "lorem ipsum";

                    public PlusThree Inner { get; } = new();

                    public sealed class PlusThree
                    {
                        public string Text
                            => "lorem ipsum";
                    }
                }
            }
        }

        public interface Consumer
        {
            string Text { get; }
            PlusOne Inner { get; }

            public interface PlusOne
            {
                string Text { get; }
                PlusTwo Inner { get; }

                public interface PlusTwo
                {
                    string Text { get; }
                    PlusThree Inner { get; }

                    public interface PlusThree
                    {
                        string Text { get; }
                    }
                }
            }
        }
    }
}
