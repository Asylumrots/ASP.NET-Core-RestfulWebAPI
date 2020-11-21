using AutoMapper;
using RestfulWebAPI.Api.Entities;
using RestfulWebAPI.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestfulWebAPI.Api.Profiles
{
    public class CompanyProfile : Profile
    {
        public CompanyProfile()
        {
            CreateMap<Company, CompanyDto>()//自动映射（但是需要名称一致）
                .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Name));//手动映射

            //从CompanyAddDto映射到Company
            CreateMap<CompanyAddDto, Company>();

            CreateMap<Company, CompanyFullDto>();

            CreateMap<CompanyAddWithBankruptTimeDto, Company>();
        }
    }
}
