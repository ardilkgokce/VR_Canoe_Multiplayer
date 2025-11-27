namespace VRCanoe.Game
{
    /// <summary>
    /// Oyun durumlari.
    /// </summary>
    public enum GameState
    {
        /// <summary>Oyuncular bekleniyor (2 oyuncu gerekli)</summary>
        WaitingForPlayers,

        /// <summary>Isim girisi ekrani</summary>
        EnteringNames,

        /// <summary>Baslamaya hazir</summary>
        Ready,

        /// <summary>3-2-1 geri sayim</summary>
        Countdown,

        /// <summary>Oyun devam ediyor</summary>
        Playing,

        /// <summary>Oyun bitti, skorboard gosteriliyor</summary>
        Finished
    }
}
