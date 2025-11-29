using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRCanoe.Game
{
    /// <summary>
    /// Tek bir skor kaydi.
    /// </summary>
    [Serializable]
    public class ScoreEntry
    {
        public string teamName;
        public int score;
        public float finishTime;
        public string date;
        public string player1Name;
        public string player2Name;

        public ScoreEntry()
        {
            teamName = "";
            score = 0;
            finishTime = 0f;
            date = "";
            player1Name = "";
            player2Name = "";
        }

        public ScoreEntry(string teamName, int score, float finishTime, string player1, string player2)
        {
            this.teamName = teamName;
            this.score = score;
            this.finishTime = finishTime;
            this.date = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            this.player1Name = player1;
            this.player2Name = player2;
        }

        /// <summary>
        /// Formatli sure stringi (MM:SS.mm).
        /// </summary>
        public string GetFormattedTime()
        {
            int minutes = Mathf.FloorToInt(finishTime / 60f);
            int seconds = Mathf.FloorToInt(finishTime % 60f);
            int milliseconds = Mathf.FloorToInt((finishTime % 1f) * 100f);
            return $"{minutes:00}:{seconds:00}.{milliseconds:00}";
        }
    }

    /// <summary>
    /// Tum skorlari tutan veri sinifi.
    /// JSON olarak serialize edilebilir.
    /// Tum skorlar saklanir, sadece gosterimde top 10 kullanilir.
    /// </summary>
    [Serializable]
    public class ScoreboardData
    {
        public List<ScoreEntry> scores = new List<ScoreEntry>();
        public int maxDisplayEntries = 10; // Sadece gosterim icin

        /// <summary>
        /// Yeni skor ekle ve sirala.
        /// Tum skorlar saklanir, silinmez.
        /// </summary>
        public bool AddScore(ScoreEntry entry)
        {
            // Ayni kayit var mi kontrol et (duplicate onleme)
            if (IsDuplicate(entry))
            {
                return false;
            }

            scores.Add(entry);
            SortScores();

            // Top 10'a girdi mi?
            int rank = scores.IndexOf(entry);
            return rank < maxDisplayEntries;
        }

        /// <summary>
        /// Ayni kayit var mi kontrol et.
        /// </summary>
        private bool IsDuplicate(ScoreEntry entry)
        {
            foreach (var existing in scores)
            {
                if (existing.teamName == entry.teamName &&
                    existing.score == entry.score &&
                    existing.date == entry.date &&
                    Mathf.Abs(existing.finishTime - entry.finishTime) < 0.01f)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Skorlari yuksekten dusuge sirala.
        /// </summary>
        public void SortScores()
        {
            scores.Sort((a, b) =>
            {
                // Oncelik: Yuksek skor
                int scoreCompare = b.score.CompareTo(a.score);
                if (scoreCompare != 0) return scoreCompare;

                // Esit skorlarda: Dusuk sure (hizli bitiren)
                return a.finishTime.CompareTo(b.finishTime);
            });
        }

        /// <summary>
        /// Top N skoru getir.
        /// </summary>
        public List<ScoreEntry> GetTopScores(int count)
        {
            int actualCount = Mathf.Min(count, scores.Count);
            return scores.GetRange(0, actualCount);
        }

        /// <summary>
        /// Tum skorlari getir.
        /// </summary>
        public List<ScoreEntry> GetAllScores()
        {
            return new List<ScoreEntry>(scores);
        }

        /// <summary>
        /// Toplam skor sayisi.
        /// </summary>
        public int TotalCount => scores.Count;

        /// <summary>
        /// Tum skorlari temizle.
        /// </summary>
        public void ClearScores()
        {
            scores.Clear();
        }

        /// <summary>
        /// Belirli bir skorun siralamasini getir (1-indexed).
        /// </summary>
        public int GetRank(ScoreEntry entry)
        {
            int index = scores.IndexOf(entry);
            return index >= 0 ? index + 1 : -1;
        }

        /// <summary>
        /// Belirli bir skorun top 10'da olup olmadigini kontrol et.
        /// </summary>
        public bool IsInTopTen(int score)
        {
            if (scores.Count < maxDisplayEntries) return true;
            return score > scores[maxDisplayEntries - 1].score;
        }
    }
}
