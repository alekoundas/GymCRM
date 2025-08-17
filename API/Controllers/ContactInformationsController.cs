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
            return await _dataService.ContactInformations.GetPaggingWithFilterAndSort(null,null,null);
        }

        // GET: api/ContactInformations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ContactInformation>> GetContactInformation(int id)
        {
            var contactInformation = await _dataService.ContactInformations.FindByIdAsync(id);

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

            _dataService.ContactInformations.Update(contactInformation);

            try
            {
                await _dataService.SaveChangesAsync();
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
            await _dataService.SaveChangesAsync();

            return CreatedAtAction("GetContactInformation", new { id = contactInformation.Id }, contactInformation);
        }

        // DELETE: api/ContactInformations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContactInformation(int id)
        {
            var contactInformation = await _dataService.ContactInformations.FindByIdAsync(id);
            if (contactInformation == null)
                return NotFound();

            _dataService.ContactInformations.Remove(contactInformation);
            await _dataService.SaveChangesAsync();

            return NoContent();
        }


        // POST: api/ContactInformations/GetDataTable
        [HttpPost("GetDataTable")]
        public async Task<ApiResponse<DataTableDto<ContactInformationDto>>> GetDataTable([FromBody] DataTableDto<ContactInformationDto> dataTable)
        {
            Func<IQueryable<ContactInformation>, IOrderedQueryable<ContactInformation>>? orderByQuery = null;
            List<Func<IOrderedQueryable<ContactInformation>, IOrderedQueryable<ContactInformation>>>? thenOrderByQuery = new List<Func<IOrderedQueryable<ContactInformation>, IOrderedQueryable<ContactInformation>>>();
            List<Expression<Func<ContactInformation, bool>>>? filterQuery = new List<Expression<Func<ContactInformation, bool>>>();
            List<Func<IQueryable<ContactInformation>, IIncludableQueryable<ContactInformation, object>>>? includesQuery = new List<Func<IQueryable<ContactInformation>, IIncludableQueryable<ContactInformation, object>>>();


            // Handle Sorting of DataTable.
            if (dataTable.MultiSortMeta?.Count() > 0)
            {
                // Create the first OrderBy().
                DataTableSortDto? dataTableSort = dataTable.MultiSortMeta.First();
                if (dataTableSort.Order > 0)
                    orderByQuery = x => x.OrderByColumn(dataTableSort.Field);
                else if (dataTableSort.Order < 0)
                    orderByQuery = x => x.OrderByColumnDescending(dataTableSort.Field);

                // Create the rest OrderBy methods as ThenBy() if any.
                foreach (var sortInfo in dataTable.MultiSortMeta.Skip(1))
                {
                    if (dataTableSort.Order > 0)
                        thenOrderByQuery.Add(x => x.ThenByColumn(sortInfo.Field));
                    else if (dataTableSort.Order < 0)
                        thenOrderByQuery.Add(x => x.ThenByColumnDescending(sortInfo.Field));
                }
            }


            // Handle Filtering of DataTable.
            if (dataTable.Filters?.Value?.Value != null && dataTable.Filters?.Value.Value.Length > 0)
                filterQuery.Add(x => x.Value.Contains(dataTable.Filters.Value.Value));

            if (dataTable.Filters?.Description?.Value != null && dataTable.Filters?.Description.Value.Length > 0)
                filterQuery.Add(x => x.Description.Contains(dataTable.Filters.Description.Value));

            if (dataTable.Filters?.CustomerId?.Value != null && dataTable.Filters?.CustomerId.Value.Length > 0)
                filterQuery.Add(x => x.CustomerId.ToString() == dataTable.Filters.CustomerId.Value);


            // Retrieve Data.
            List<ContactInformation> result = await _dataService.ContactInformations.GetPaggingWithFilterAndSort(
                filterQuery,
                orderByQuery,
                thenOrderByQuery,
                includesQuery,
                dataTable.PageCount.Value,
                dataTable.Page.Value
            );
            List<ContactInformationDto> customerDto = _mapper.Map<List<ContactInformationDto>>(result);

            int rows = await _dataService.ContactInformations.CountAsyncFiltered(filterQuery);

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
