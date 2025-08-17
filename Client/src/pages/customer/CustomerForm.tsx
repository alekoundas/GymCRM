import React, { useEffect, useState } from "react";
import { Button } from "primereact/button";
import { Card } from "primereact/card";
import { DataTable } from "primereact/datatable";
import { Column, ColumnBodyOptions } from "primereact/column";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { CustomerDto } from "../../model/CustomerDto";
import { ContactInformationTypesEnum } from "../../enum/ContactInformationTypesEnum";
import { InputText } from "primereact/inputtext";
import ApiService from "../../services/ApiService";
import { Dialog } from "primereact/dialog";
import { InputTextarea } from "primereact/inputtextarea";
import { ContactInformationDto } from "../../model/ContactInformationDto";
import { Dropdown, DropdownChangeEvent } from "primereact/dropdown";
import { InputMask } from "primereact/inputmask";
import { TicketDto } from "../../model/TicketDto";
import { InputNumber } from "primereact/inputnumber";
import { Editor, EditorTextChangeEvent } from "primereact/editor";
import { TicketTypesEnum } from "../../enum/TicketTypesEnum";
import { TicketStatusEnum } from "../../enum/TicketStatusEnum";
import { DataTableDto } from "../../model/DataTableDto";
import DataTableService from "../../services/DataTableService";
import { ToastService } from "../../services/ToastService";

enum FormType {
  Customer,
  Tickets,
  ContactInformation,
}
enum FormMode {
  ADD = "ADD",
  VIEW = "VIEW",
  EDIT = "EDIT",
}

interface DynamicColumns {
  field: string;
  header: string;
  sortable: boolean;
  filter: boolean;
  filterPlaceholder: string;
  style: React.CSSProperties;
  body: any | null;
}

export default function CustomerForm() {
  const navigate = useNavigate();
  const params = useParams();
  const location = useLocation();

  const formMode: FormMode = window.location.href.endsWith("/add")
    ? FormMode.ADD
    : window.location.href.endsWith("/edit")
    ? FormMode.EDIT
    : FormMode.VIEW;

  const [customer, setCustomer] = useState(new CustomerDto());
  const [ticket, setTicket] = useState(new TicketDto());
  const [ticketLoading, setTicketLoading] = useState(true);
  const [contactInformation, setContactInformation] = useState(
    new ContactInformationDto()
  );
  const [contactInformationLoading, setContactInformationLoading] =
    useState(true);
  const [ticketDataTableDto, setTicketDataTableDto] = useState<
    DataTableDto<TicketDto>
  >({
    data: [],
    first: 0,
    rows: 10,
    page: 1,
    pageCount: 0,
    multiSortMeta: [{ field: "id", order: -1 }],
    filters: {
      id: { value: "", matchMode: "contains" },
      description: { value: "", matchMode: "contains" },
      completedOn: { value: "", matchMode: "contains" },
      customerId: { value: "", matchMode: "contains" },
    },
  });

  const [contactInformationDataTableDto, setContactInformationDataTableDto] =
    useState<DataTableDto<ContactInformationDto>>({
      data: [],
      first: 0,
      rows: 10,
      page: 1,
      pageCount: 0,
      multiSortMeta: [],
      filters: {
        value: { value: "", matchMode: "contains" },
        description: { value: "", matchMode: "contains" },
        customerId: { value: "", matchMode: "contains" },
      },
    });

  const ticketDataTableService = new DataTableService(
    "tickets",
    ticketDataTableDto,
    setTicketDataTableDto,
    setTicketLoading,
    null
  );

  const contactInformationDataTableService = new DataTableService(
    "contactInformations",
    contactInformationDataTableDto,
    setContactInformationDataTableDto,
    setContactInformationLoading,
    null
  );

  const [elementsVisibility, setElementsVisibility] = useState({
    contactInformationVisible: false,
    contactInformationDeleteVisible: false,
    contactInformationPhoneVisible: true,
    contactInformationEmailVisible: false,
    ticketVisible: false,
  });

  //
  //  Load initial data on load and everytime url changes
  //
  useEffect(() => {
    if (params["id"]) {
      const id = params["id"];
      ApiService.get<CustomerDto>("customers", id).then((result) => {
        if (result) setCustomer(result);
      });

      // Add filter on parent Id (CustomerId)
      ticketDataTableDto.filters.customerId = {
        value: id,
        matchMode: "contains",
      };
      contactInformationDataTableDto.filters.customerId = {
        value: id,
        matchMode: "contains",
      };
      ticketDataTableService.loadData(null);
      contactInformationDataTableService.loadData(null);
    } else {
      setTicketLoading(false);
      setContactInformationLoading(false);
    }

    console.log("loaded");
  }, [location]);

  //
  // Handle input changes.
  //
  const handleChange = (
    event: React.ChangeEvent<any> | DropdownChangeEvent,
    formType: FormType
  ) => {
    const name = event.target.name;
    const value = event.target.value;

    if (formType === FormType.Customer) {
      customer[name] = value;
      setCustomer({ ...customer });
    } else if (formType === FormType.ContactInformation) {
      contactInformation[name] = value;
      setContactInformation({ ...contactInformation });
    } else if (formType === FormType.Tickets) {
      ticket[name] = value;
      setTicket({ ...ticket });
    }
  };

  const handleEditorChange = (event: EditorTextChangeEvent) => {
    ticket.descriptionHTML = event.htmlValue ?? "";
    ticket.description = event.textValue ?? "";
    setTicket({ ...ticket });
  };

  const handleContactInformationChange = (event: any) => {
    const value = event.target.value;
    const name = event.target.name;

    contactInformation[name] = value;
    if (name === "type" && value === ContactInformationTypesEnum.PHONE) {
      elementsVisibility.contactInformationEmailVisible = false;
      elementsVisibility.contactInformationPhoneVisible = true;
      setElementsVisibility({ ...elementsVisibility });
    }

    if (name === "type" && value === ContactInformationTypesEnum.EMAIL) {
      elementsVisibility.contactInformationEmailVisible = true;
      elementsVisibility.contactInformationPhoneVisible = false;
      setElementsVisibility({ ...elementsVisibility });
    }

    setContactInformation({ ...contactInformation });
  };

  //
  // Handle save.
  //

  const handleContactInformationSave = () => {
    // Edit mode.
    if (contactInformation.id > 0) {
      ApiService.update(
        "contactinformations",
        contactInformation,
        contactInformation.id
      ).then(() => {
        ToastService.showSuccess("");
        contactInformationDataTableService.loadData(null);
      });
    }

    // Add mode.
    else if (contactInformation.id == 0) {
      if (customer.contactInformations.length >= 1) {
        const minContactInformation = customer.contactInformations.reduce(
          (x, y) => (x.id < y.id ? x : y)
        );

        if (minContactInformation.id < 0)
          contactInformation.id = minContactInformation.id - 1;
      } else {
        contactInformation.id = -1;
      }

      contactInformationDataTableDto.data.push(contactInformation);
      setContactInformationDataTableDto({ ...contactInformationDataTableDto });
    }

    // Add mode edit from grid row action.
    else if (contactInformation.id < 0) {
      const index = customer.contactInformations.findIndex(
        (x) => x.id === contactInformation.id
      );
      customer.contactInformations[index] = contactInformation;
    }

    elementsVisibility.contactInformationVisible = false;
    setElementsVisibility({ ...elementsVisibility });
  };

  const handleTicketSave = () => {
    // Edit mode.
    if (ticket.id > 0) {
      ApiService.update("tickets", ticket, ticket.id).then(() => {
        ToastService.showSuccess("");
        ticketDataTableService.loadData(null);
      });
    }

    // Add mode.
    else if (ticket.id == 0) {
      // Get min id and set as minId-1
      if (customer.tickets.length >= 1) {
        const minId = customer.tickets
          .map((x) => x.id)
          .reduce((x, y) => (x < y ? x : y));

        if (minId < 0) ticket.id = minId - 1;
      } else {
        ticket.id = -1;
      }

      ticketDataTableDto.data.push(ticket);
      setTicketDataTableDto({ ...ticketDataTableDto });
    }
    // Add mode edit from grid row action.
    else if (ticket.id < 0) {
      const index = customer.tickets.findIndex((x) => x.id === ticket.id);
      customer.tickets[index] = ticket;
    }

    elementsVisibility.ticketVisible = false;
    setElementsVisibility({ ...elementsVisibility });
  };

  const handleSubmit = (event: any) => {
    event.preventDefault();

    if (customer.id > 0) {
      ApiService.update("customers", customer, customer.id);
    } else {
      customer.tickets = ticketDataTableDto.data;
      customer.contactInformations = contactInformationDataTableDto.data;

      ApiService.create("customers", customer).then((x) => {
        ToastService.showSuccess("");
        navigate("/customers/" + x?.id + "/view");
      });
    }
  };

  //
  // Handle grid row actions.
  //
  const handleEditContactInformation = (rowData: ContactInformationDto) => {
    setContactInformation({ ...rowData });

    elementsVisibility.contactInformationVisible = true;
    setElementsVisibility({ ...elementsVisibility });
  };

  const handleDeleteContactInformation = (rowData: ContactInformationDto) => {
    // Find customer index and delete him.
    const index = customer.contactInformations.indexOf(rowData, 0);
    if (index > -1) {
      customer.contactInformations.splice(index, 1);
    }
    setCustomer({ ...customer });

    elementsVisibility.contactInformationDeleteVisible = true;
    setElementsVisibility({ ...elementsVisibility });
  };

  const handleEditTicket = (rowData: TicketDto) => {
    setTicket({ ...rowData });

    elementsVisibility.ticketVisible = true;
    setElementsVisibility({ ...elementsVisibility });
  };

  const handleDeleteTicket = (rowData: TicketDto) => {
    // Find customer index and delete him.
    const index = customer.tickets.indexOf(rowData, 0);
    if (index > -1) {
      customer.tickets.splice(index, 1);
    }
    setCustomer({ ...customer });

    elementsVisibility.contactInformationDeleteVisible = true;
    setElementsVisibility({ ...elementsVisibility });
  };

  //
  //  Handle modals open/close.
  //
  const openAddContactInformation = () => {
    setContactInformation(new ContactInformationDto());
    elementsVisibility.contactInformationVisible = true;
    setElementsVisibility({ ...elementsVisibility });
  };

  const openAddTicket = () => {
    setTicket(new TicketDto());
    elementsVisibility.ticketVisible = true;
    setElementsVisibility({ ...elementsVisibility });
  };

  const onHideModal = () => {
    elementsVisibility.contactInformationVisible = false;
    elementsVisibility.ticketVisible = false;
    elementsVisibility.contactInformationDeleteVisible = false;

    setElementsVisibility({ ...elementsVisibility });
  };

  //
  // Handle HTML templates
  //
  const renderHeader = (formType: FormType) => {
    let onClickFn: any = () => {};

    if (formType === FormType.ContactInformation) {
      onClickFn = openAddContactInformation;
    } else if (formType === FormType.Tickets) {
      onClickFn = openAddTicket;
    }
    return (
      <div className="flex justify-content-between">
        <div></div>
        <Button
          type="button"
          icon="pi pi-plus"
          label="Add"
          outlined
          onClick={onClickFn}
        />
      </div>
    );
  };

  const dialogFooter = (formType: FormType) => (
    <React.Fragment>
      <Button
        label="Cancel"
        icon="pi pi-times"
        outlined
        onClick={onHideModal}
      />
      {formType === FormType.ContactInformation && (
        <Button
          label="Save"
          icon="pi pi-check"
          onClick={handleContactInformationSave}
        />
      )}
      {formType === FormType.Tickets && (
        <Button
          label="Save"
          icon="pi pi-check"
          onClick={handleTicketSave}
        />
      )}
    </React.Fragment>
  );

  const contactInformationGridRowActions = (
    rowData: ContactInformationDto,
    _options: ColumnBodyOptions
  ) => (
    <React.Fragment>
      <Button
        icon="pi pi-pencil"
        rounded
        outlined
        className="mr-2"
        onClick={() => handleEditContactInformation(rowData)}
      />
      <Button
        icon="pi pi-trash"
        rounded
        outlined
        severity="danger"
        onClick={() => handleDeleteContactInformation(rowData)}
      />
    </React.Fragment>
  );

  const ticketGridRowActions = (rowData: TicketDto) => (
    <React.Fragment>
      <Button
        icon="pi pi-pencil"
        rounded
        outlined
        className="mr-2"
        onClick={() => handleEditTicket(rowData)}
      />
      <Button
        icon="pi pi-trash"
        rounded
        outlined
        severity="danger"
        onClick={() => handleDeleteTicket(rowData)}
      />
    </React.Fragment>
  );

  const contactInformationDataTableColumns: DynamicColumns[] = [
    {
      field: "type",
      header: "Type",
      sortable: formMode !== FormMode.ADD,
      filter: formMode !== FormMode.ADD,
      filterPlaceholder: "Search",
      style: { width: "10%" },
      body: null,
    },
    {
      field: "value",
      header: "Value",
      sortable: formMode !== FormMode.ADD,
      filter: formMode !== FormMode.ADD,
      filterPlaceholder: "Search",
      style: { width: "20%" },
      body: null,
    },
    {
      field: "description",
      header: "Description",
      sortable: formMode !== FormMode.ADD,
      filter: formMode !== FormMode.ADD,
      filterPlaceholder: "Search",
      style: { width: "20%" },
      body: null,
    },
    {
      field: "",
      header: "Actions",
      sortable: false,
      filter: false,
      filterPlaceholder: "Search",
      style: { width: "20%" },
      body: contactInformationGridRowActions,
    },
  ];

  const ticketDataTableColumns: DynamicColumns[] = [
    {
      field: "id",
      header: "Id",
      sortable: formMode !== FormMode.ADD,
      filter: formMode !== FormMode.ADD,
      filterPlaceholder: "Search",
      style: { width: "10%" },
      body: "",
    },
    {
      field: "description",
      header: "Description",
      sortable: formMode !== FormMode.ADD,
      filter: formMode !== FormMode.ADD,
      filterPlaceholder: "Search",
      style: { width: "20%" },
      body: "",
    },
    {
      field: "completedOn",
      header: "Completed On",
      sortable: formMode !== FormMode.ADD,
      filter: formMode !== FormMode.ADD,
      filterPlaceholder: "Search",
      style: { width: "20%" },
      body: "",
    },
    {
      field: "",
      header: "Actions",
      sortable: false,
      filter: false,
      filterPlaceholder: "Search",
      style: { width: "20%" },
      body: ticketGridRowActions,
    },
  ];

  return (
    <>
      <div className="">
        <Card title="Customer ">
          <form onSubmit={handleSubmit}>
            <div className="flex align-items-center justify-content-center">
              <div className="card flex flex-column md:flex-row gap-3">
                <div className="p-inputgroup flex-1">
                  <span className="p-float-label">
                    <InputText
                      id="firstName"
                      name="firstName"
                      value={customer.firstName}
                      onChange={(e) => handleChange(e, FormType.Customer)}
                    />
                    <label htmlFor="firstName">First Name</label>
                  </span>
                </div>

                <div className="p-inputgroup flex-1">
                  <span className="p-float-label">
                    <InputText
                      id="lastName"
                      name="lastName"
                      value={customer.lastName}
                      onChange={(e) => handleChange(e, FormType.Customer)}
                    />
                    <label htmlFor="lastName">Last Name</label>
                  </span>
                </div>
              </div>
            </div>

            {/* <div className="flex align-items-center justify-content-center">
              <div className="card flex flex-column md:flex-row gap-3">
                <div className="p-inputgroup flex-1">
                  <span className="p-inputgroup-addon">
                    <i className="pi pi-user"></i>
                  </span>
                  <InputText
                    placeholder="First Name"
                    value={customer.FirstName}
                  />
                </div>

                <div className="p-inputgroup flex-1">
                  <span className="p-inputgroup-addon">$</span>
                  <InputNumber placeholder="Price" />
                  <span className="p-inputgroup-addon">.00</span>
                </div>
              </div>
            </div> */}

            <input type="submit" />
          </form>
        </Card>

        <div className="grid mt-2  ">
          <div className="col-12 lg:col-7 ">
            <Card
              title="Tickets "
              className=""
            >
              <DataTable
                className="w-full"
                value={ticketDataTableDto.data}
                lazy
                stripedRows
                emptyMessage="No tickets found."
                tableStyle={{ minWidth: "50rem" }}
                selectionMode="single"
                loading={ticketLoading}
                // Pagging.
                paginator
                rows={ticketDataTableDto.rows}
                totalRecords={ticketDataTableDto.pageCount}
                onPage={ticketDataTableService.onPage}
                rowsPerPageOptions={[10, 25, 50, 100]}
                // paginatorLeft={paginatorLeft}
                currentPageReportTemplate={
                  "1 to " +
                  ticketDataTableDto.rows +
                  " out of " +
                  ticketDataTableDto.pageCount
                }
                paginatorTemplate="FirstPageLink PrevPageLink CurrentPageReport NextPageLink LastPageLink RowsPerPageDropdown"
                // Filter.
                filterDisplay="row"
                filters={ticketDataTableDto.filters}
                onFilter={ticketDataTableService.onFilter}
                // Sort.
                removableSort
                sortMode="multiple"
                onSort={ticketDataTableService.onSort}
                multiSortMeta={ticketDataTableDto.multiSortMeta}
                header={renderHeader(FormType.Tickets)}
              >
                {ticketDataTableColumns.map((col, _i) => (
                  <Column
                    key={col.field}
                    field={col.field}
                    header={col.header}
                    sortable={col.sortable}
                    filter={col.filter}
                    filterPlaceholder={col.filterPlaceholder}
                    style={col.style}
                    body={col.body}
                  ></Column>
                ))}
              </DataTable>
            </Card>
          </div>

          <div className="col-12 lg:col-5">
            <Card
              title="Contact Information "
              className=""
            >
              <DataTable
                className="w-full"
                value={contactInformationDataTableDto.data}
                lazy
                stripedRows
                emptyMessage="No contact informations found."
                tableStyle={{ minWidth: "50rem" }}
                selectionMode="single"
                loading={contactInformationLoading}
                // Pagging.
                paginator
                rows={contactInformationDataTableDto.rows}
                totalRecords={contactInformationDataTableDto.pageCount}
                onPage={contactInformationDataTableService.onPage}
                rowsPerPageOptions={[10, 25, 50, 100]}
                // paginatorLeft={paginatorLeft}
                currentPageReportTemplate={
                  "1 to " +
                  contactInformationDataTableDto.rows +
                  " out of " +
                  contactInformationDataTableDto.pageCount
                }
                paginatorTemplate="FirstPageLink PrevPageLink CurrentPageReport NextPageLink LastPageLink RowsPerPageDropdown"
                // Filter.
                filterDisplay="row"
                filters={contactInformationDataTableDto.filters}
                onFilter={contactInformationDataTableService.onFilter}
                // Sort.
                removableSort
                sortMode="multiple"
                onSort={contactInformationDataTableService.onSort}
                multiSortMeta={contactInformationDataTableDto.multiSortMeta}
                header={renderHeader(FormType.ContactInformation)}
              >
                {contactInformationDataTableColumns.map((col, _i) => (
                  <Column
                    key={col.field}
                    field={col.field}
                    header={col.header}
                    sortable={col.sortable}
                    filter={col.filter}
                    filterPlaceholder={col.filterPlaceholder}
                    style={col.style}
                    body={col.body}
                  ></Column>
                ))}
              </DataTable>
            </Card>
          </div>
        </div>
      </div>

      {/*                                   */}
      {/*      Modal of Ticket Form         */}
      {/*                                   */}
      <Dialog
        visible={elementsVisibility.ticketVisible}
        style={{ width: "35%" }}
        header="Add Ticket"
        modal
        className="p-fluid"
        footer={dialogFooter(FormType.Tickets)}
        onHide={onHideModal}
      >
        <div className="field">
          <label htmlFor="id">Id</label>
          <InputNumber
            id="id"
            name="Id"
            value={ticket.id}
            disabled
          />
        </div>
        <div className="field">
          <label htmlFor="type">Type</label>
          <Dropdown
            id="type"
            name="type"
            value={ticket.type}
            onChange={(e) => handleChange(e, FormType.Tickets)}
            options={Object.keys(TicketTypesEnum)}
            optionLabel="type"
            placeholder="Select Type"
            className="w-full md:w-14rem"
            checkmark={true}
            highlightOnSelect={true}
          />
        </div>
        <div className="field">
          <label htmlFor="status">Status</label>
          <Dropdown
            id="status"
            name="status"
            value={ticket.status}
            onChange={(e) => handleChange(e, FormType.Tickets)}
            options={Object.keys(TicketStatusEnum)}
            optionLabel="type"
            placeholder="Select Type"
            className="w-full md:w-14rem"
            checkmark={true}
            highlightOnSelect={true}
          />
        </div>
        <div className="field">
          <label htmlFor="description">Description</label>
          <Editor
            id="description"
            name="description"
            value={ticket.descriptionHTML}
            onTextChange={handleEditorChange}
            style={{ height: "320px" }}
          />
        </div>
        {/* multiple fields */}
        {/* 
        <div className="flex align-items-center justify-content-center">
          <div className="card flex flex-column md:flex-row gap-3">
            <div className="p-inputgroup flex-1"></div>
            <div className="p-inputgroup flex-1"></div>
          </div>
        </div> */}
      </Dialog>

      {/*                                   */}
      {/* Modal of Contact Information Form */}
      {/*                                   */}
      <Dialog
        visible={elementsVisibility.contactInformationVisible}
        style={{ width: "35%" }}
        header="Add Contact Information"
        modal
        className="p-fluid"
        footer={dialogFooter(FormType.ContactInformation)}
        onHide={onHideModal}
      >
        <div className="field">
          <label htmlFor="type">Type</label>
          <Dropdown
            id="type"
            name="type"
            value={contactInformation.type}
            onChange={handleContactInformationChange}
            options={Object.keys(ContactInformationTypesEnum)}
            optionLabel="type"
            placeholder="Select Type"
            className="w-full md:w-14rem"
            checkmark={true}
            highlightOnSelect={true}
          />
        </div>

        <div className="field">
          <label htmlFor="value">Value</label>
          {elementsVisibility.contactInformationPhoneVisible && (
            <InputMask
              id="value"
              name="value"
              value={contactInformation.value}
              onChange={handleContactInformationChange}
              mask="999 9999 999"
              placeholder="999 9999 999"
              required
              autoFocus
              // className={classNames({ "p-invalid": submitted && !product.name })}
            />
          )}
          {elementsVisibility.contactInformationEmailVisible && (
            <InputText
              id="value"
              name="value"
              value={contactInformation.value}
              onChange={(e) => handleChange(e, FormType.ContactInformation)}
              // placeholder={inputMaskSettings.placeholder}
              required
              autoFocus
              // className={classNames({ "p-invalid": submitted && !product.name })}
            />
          )}
          {/* {submitted && !product.name && (
            <small className="p-error">Name is required.</small>
          )} */}
        </div>
        <div className="field">
          <label htmlFor="description">Description</label>
          <InputTextarea
            id="description"
            name="description"
            value={contactInformation.description}
            onChange={(e) => handleChange(e, FormType.ContactInformation)}
            required
            rows={3}
            cols={20}
          />
        </div>
      </Dialog>

      {/*                                   */}
      {/*        Modal for Delete           */}
      {/*                                   */}
      <Dialog
        visible={elementsVisibility.contactInformationDeleteVisible}
        style={{ width: "35%" }}
        header="Confirm"
        modal
        className="p-fluid"
        footer={dialogFooter(FormType.Customer)}
        onHide={onHideModal}
      >
        <div className="confirmation-content">
          <i
            className="pi pi-exclamation-triangle mr-3"
            style={{ fontSize: "2rem" }}
          />
          <span>Are you sure you want to delete ?</span>
        </div>
      </Dialog>
    </>
  );
}
