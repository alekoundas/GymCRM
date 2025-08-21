using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Core.Models;
using Business.Services;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
using Core.Dtos.DataTable;
using AutoMapper;
using Core.Dtos.Identity;
using Core.Dtos;
using Business;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Core.Enums;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactInformationsController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IDataService _dataService;
        private readonly ILogger<ContactInformationsController> _logger;

        public ContactInformationsController(ILogger<ContactInformationsController> logger, IDataService dataService, IMapper mapper)
        {
            _mapper = mapper;
            _logger = logger;
            _dataService = dataService;
        }

        // GET: api/ContactInformations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ContactInformation>>> GetContactInformations()
        {
            return await _dataService.ContactInformations.ToListAsync();
        }

        // GET: api/ContactInformations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ContactInformation>> GetContactInformation(int id)
        {
            var contactInformation = await _dataService.ContactInformations.FindAsync(id);

            if (contactInformation == null)
                return NotFound();

            return contactInformation;
        }

        // PUT: api/ContactInformations/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutContactInformation(int id, ContactInformation contactInformation)
        {
            if (id != contactInformation.Id)
                return BadRequest();


            try
            {
                _dataService.Update(contactInformation);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ContactInformationExists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // POST: api/ContactInformations
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ContactInformation>> PostContactInformation(ContactInformation contactInformation)
        {
            _dataService.ContactInformations.Add(contactInformation);

            return CreatedAtAction("GetContactInformation", new { id = contactInformation.Id }, contactInformation);
        }

        // DELETE: api/ContactInformations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContactInformation(int id)
        {
            var contactInformation = await _dataService.ContactInformations.FindAsync(id);
            if (contactInformation == null)
                return NotFound();

            _dataService.ContactInformations.Remove(contactInformation);

            return NoContent();
        }


        // POST: api/ContactInformations/GetDataTable
        [HttpPost("GetDataTable")]
        public async Task<ApiResponse<DataTableDto<ContactInformationDto>>> GetDataTable([FromBody] DataTableDto<ContactInformationDto> dataTable)
        {

            List<Func<IOrderedQueryable<ContactInformation>, IOrderedQueryable<ContactInformation>>>? thenOrderByQuery = new List<Func<IOrderedQueryable<ContactInformation>, IOrderedQueryable<ContactInformation>>>();
            List<Expression<Func<ContactInformation, bool>>>? filterQuery = new List<Expression<Func<ContactInformation, bool>>>();
            List<Func<IQueryable<ContactInformation>, IIncludableQueryable<ContactInformation, object>>>? includesQuery = new List<Func<IQueryable<ContactInformation>, IIncludableQueryable<ContactInformation, object>>>();

            var query = _dataService.ContactInformations;

            // Handle Sorting of DataTable.
            if (dataTable.MultiSortMeta?.Count() > 0)
            {
                // Create the first OrderBy().
                DataTableSortDto? dataTableSort = dataTable.MultiSortMeta.First();
                if (dataTableSort.Order > 0)
                    query.OrderBy(dataTableSort.Field, OrderDirectionEnum.ASCENDING);
                else if (dataTableSort.Order < 0)
                    query.OrderBy(dataTableSort.Field, OrderDirectionEnum.DESCENDING);

                // Create the rest OrderBy methods as ThenBy() if any.
                foreach (var sortInfo in dataTable.MultiSortMeta.Skip(1))
                {
                    if (dataTableSort.Order > 0)
                        query.ThenBy(sortInfo.Field, OrderDirectionEnum.ASCENDING);
                    else if (dataTableSort.Order < 0)
                        query.ThenBy(sortInfo.Field, OrderDirectionEnum.DESCENDING);
                }
            }

           
            int skip = (dataTable.Page - 1) * dataTable.Rows;
            int take = dataTable.Rows;

            query.AddPagging(skip, take);
            // Retrieve Data.
            List<ContactInformation> result = await query.ToListAsync();
            List<ContactInformationDto> customerDto = _mapper.Map<List<ContactInformationDto>>(result);

            //TODO add filter
            int rows = await _dataService.ContactInformations.CountAsync();

            dataTable.Data = customerDto;
            dataTable.PageCount = rows;

            return new ApiResponse<DataTableDto<ContactInformationDto>>().SetSuccessResponse(dataTable);
        }

        private bool ContactInformationExists(int id)
        {
            return _dataService.ContactInformations.Any(e => e.Id == id);
        }
    }
}
