using System;
using System.Collections.Generic;

namespace Antiplag.Analysis.Data.Entities;

public class SubmissionEntity
{
    public Guid SubmissionId { get; set; }

    public string WorkId { get; set; } = null!;
    public string StudentId { get; set; } = null!;

    public Guid FileId { get; set; }

    public string Hash { get; set; } = null!;

    /// <summary>
    /// Набор шинглов (k-грамм по словам) для вычисления Jaccard similarity.
    /// Храним в Postgres как text[].
    /// </summary>
    public string[] Shingles { get; set; } = Array.Empty<string>();

    public DateTime CreatedAt { get; set; }

    public ICollection<ReportEntity> Reports { get; set; } = new List<ReportEntity>();
}
