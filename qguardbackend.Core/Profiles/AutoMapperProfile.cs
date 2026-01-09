using AutoMapper;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.ResponseDto;
using qguardbackend.Data.Entities;
using examportal.Data.Entities;
using System.Data;

namespace qguardbackend.Core.Profiles;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<ApplicationUser, ApplicationUserResponse>().ReverseMap();
        CreateMap<ApplicationUser, ApplicationUserSignUpResponse>().ReverseMap();
        CreateMap<Institution, InstitutionResponseDto>().ReverseMap();
        CreateMap<Program, ProgramModel>().ReverseMap();


    }
}