using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using RestfulWebAPI.Api.Entities;
using RestfulWebAPI.Api.Helpers;
using RestfulWebAPI.Api.Models;
using RestfulWebAPI.Api.Services;

namespace RestfulWebAPI.Api.Controllers
{
    [Route("api/companycollections")]
    [ApiController]
    public class CompanyCollectionsController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly ICompanyRepository _companyRepository;

        public CompanyCollectionsController(IMapper mapper, ICompanyRepository companyRepository)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _companyRepository = companyRepository ?? throw new ArgumentNullException(nameof(companyRepository));
        }

        [HttpGet("{ids}", Name = nameof(GetCompanyCollection))]
        public async Task<ActionResult<IEnumerable<Company>>> GetCompanyCollection
            ([FromRoute][ModelBinder(BinderType = typeof(ArrayModelBinder))]IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                return BadRequest();
            }

            var entites = await _companyRepository.GetCompaniesAsync(ids);

            //当数量不相等时表示错误
            if (ids.Count() != entites.Count())
            {
                return NotFound();
            }
            var dtosToReturn = _mapper.Map<IEnumerable<CompanyDto>>(entites);

            return Ok(dtosToReturn);
        }

        [HttpPost]
        public async Task<ActionResult<IEnumerable<CompanyDto>>> CreateCompanyCollection
            (IEnumerable<CompanyAddDto> companyCollection)
        {
            var companyEntites = _mapper.Map<IEnumerable<Company>>(companyCollection);

            foreach (var item in companyEntites)
            {
                _companyRepository.AddCompany(item);
            }

            await _companyRepository.SaveAsync();

            var dtosToReturn = _mapper.Map<IEnumerable<CompanyDto>>(companyEntites);

            var idsString = string.Join(",", dtosToReturn.Select(x => x.Id));

            return CreatedAtRoute(nameof(GetCompanyCollection),new { ids=idsString}, dtosToReturn);
        }
    }
}