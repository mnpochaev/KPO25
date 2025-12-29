using System;

namespace Antiplag.Analysis.Data.Entities;

public class ReportEntity
{
    public Guid ReportId { get; set; }

    public Guid SubmissionId { get; set; }
    public SubmissionEntity Submission { get; set; } = null!;

    public bool Plagiarism { get; set; }
    public double Similarity { get; set; }

    public Guid? OriginalSubmissionId { get; set; }
    public string? OriginalStudentId { get; set; }

    public string? WordCloudUrl { get; set; }

    public DateTime CreatedAt { get; set; }
}
