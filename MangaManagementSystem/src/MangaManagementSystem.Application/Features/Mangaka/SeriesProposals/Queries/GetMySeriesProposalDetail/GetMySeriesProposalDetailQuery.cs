using System;
using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.SeriesProposals.Queries.GetMySeriesProposalDetail;

public sealed record GetMySeriesProposalDetailQuery(
    Guid ActorUserId,
    Guid SeriesProposalId)
    : IRequest<MangakaSeriesProposalDto?>;
