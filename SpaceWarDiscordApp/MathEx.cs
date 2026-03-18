namespace SpaceWarDiscordApp;

public static class MathEx
{
    public static int Modulo(int input, int modulus) => (input % modulus + modulus) % modulus;
}