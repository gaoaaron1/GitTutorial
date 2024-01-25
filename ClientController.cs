using InteractHealthProDatabase.Data;
using InteractHealthProDatabase.Models;
using iText.Forms;
using iText.Kernel.Pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InteractHealthProDatabase.Models.Enums;

namespace InteractHealthProDatabase.Controllers
{
    [Authorize(Roles = "Admin, Manager")]
    public class ClientController : Controller
    {

        private readonly IhpDbContext _context;

        public ClientController(IhpDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string search)
        {
            if (_context.Clients == null)
            {
                return Problem("Entity set 'IhpDbContext.Clients' is null.");
            }

            IQueryable<Client> clients = _context.Clients.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                clients = clients.Where(c =>
                    c.ContactName.ToLower().Contains(search) ||
                    c.Email.ToLower().Contains(search) ||
                    c.CellPhone.ToLower().Contains(search) ||
                    c.Telephone.ToLower().Contains(search) ||
                    c.Fax.ToLower().Contains(search) ||
                    c.Address.ToLower().Contains(search) ||
                    c.City.ToLower().Contains(search) ||
                    c.Region.ToLower().Contains(search) ||
                    c.Country.ToLower().Contains(search) ||
                    c.PostalCode.ToLower().Contains(search)
                );
            }

            return View(await clients.ToListAsync());
        }


        // GET: Client/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Clients == null)
            {
                return NotFound();
            }

            var client = await _context.Clients

                .FirstOrDefaultAsync(m => m.Id == id);
            if (client == null)
            {
                return NotFound();
            }

            // Artour : added to view attributes
            client.ClientMVA = await _context.ClientMVA.Where(cmva => cmva.Client.Id == id).FirstOrDefaultAsync();

            // Load the appointments for the client
            client.Appointments = await _context.Appointments.Where(a => a.Client.Id == id).ToListAsync();

            // Load the appointments for the client
            client.HealthFile = await _context.HealthFiles.Where(hf => hf.Client.Id == id).ToListAsync();

            // Load the medical history for client
            client.MedicalHistoryAccident = await _context.MedicalHistoryAccident
            .Include(mha => mha.MedicalHistoryPreAccident)
            .Include(mha => mha.MedicalHistoryPostAccident)
            .Where(mha => mha.Client.Id == id)
            .FirstOrDefaultAsync();


            //client.InsuranceClaim = await _context.InsuranceClaims.Where(ic => ic.Client.Id == id).FirstOrDefaultAsync();

            client.Dependent = await _context.Dependent.Where(cdep => cdep.Client.Id == id).FirstOrDefaultAsync();

            client.Pet = await _context.Pet.Where(cpet => cpet.Client.Id == id).FirstOrDefaultAsync();

            client.AccidentDetail = await _context.AccidentDetails.Where(cacc => cacc.Client.Id == id).FirstOrDefaultAsync();

            AccidentVehicle? accidentVehicle = null;
            if (client.AccidentDetail != null)
            {
                // Fetch the AccidentVehicle based on the AccidentDetail
                accidentVehicle = await _context.AccidentVehicles
                .Where(av => av.AccidentDetail.Id == client.AccidentDetail.Id)
                .FirstOrDefaultAsync();
            }


            client.WorkHistory = await _context.WorkHistory.Where(cacc => cacc.Client.Id == id).FirstOrDefaultAsync();

            client.BodyTrauma = await _context.BodyTrauma.Where(cbdtrauma => cbdtrauma.Client.Id == id).FirstOrDefaultAsync();
            // Artour : end of added code

            client.BodyPart = await _context.BodyPart.FirstOrDefaultAsync(bp => bp.Client.Id == client.Id);

            client.Concussion = await _context.Concussion.FirstOrDefaultAsync(con => con.Client.Id == client.Id);

            client.Psychotherapy = await _context.Psychotherapies.FirstOrDefaultAsync(ps => ps.Client.Id == client.Id);

            client.Documents = await _context.Documents.Where(d => d.Client.Id == client.Id).ToListAsync();

   
            //client.InsuranceClaims = await _context.InsuranceClaims.Where(ic => ic.Client.Id == id).ToListAsync();

            client.InsuranceClaims = await _context.InsuranceClaims
                   .Where(ic => ic.Client.Id == id)
                    .Include(ic => ic.InsuranceCompany) // Include the associated insurance company
                    .ToListAsync();


            // Create a view model and populate client properties using reflection
            var viewModel = new ClientDetailsViewModel
            {
                AccidentVehicle = accidentVehicle
            };

            var clientProperties = typeof(Client).GetProperties();
            foreach (var property in clientProperties)
            {
                // Check if the property exists in the view model
                var viewModelProperty = typeof(ClientDetailsViewModel).GetProperty(property.Name);
                if (viewModelProperty != null)
                {
                    // Set the value from the client to the view model, handling null values
                    var clientValue = property.GetValue(client);
                    viewModelProperty.SetValue(viewModel, clientValue);
                }
            }

            return View(viewModel);


        }

        // GET: Client/Create
        public IActionResult Create()
        {

            return View();
        }

        // POST: Client/CreateOrEdit
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> CreateOrEdit(int? id, [Bind("Id,ClientMVA,Appointments,HealthFile,Pet,Dependent,InsuranceClaim,Referral,ReferralDate,DateOfLoss,ContactName,Email,CellPhone,Telephone,Fax,Address,City,Region,Country,PostalCode,Status")] Client client)

        {
            // In the Bind annotation above, we have to include the children models, otherwise they will not be included

            // The client of the children models is null, so we have to exclude it from the validation.

            ModelState.Remove("Pet");
            ModelState.Remove("Appointments");
            ModelState.Remove("HealthFile");
            ModelState.Remove("Dependent");
            ModelState.Remove("ClientMVA");
            ModelState.Remove("InsuranceClaim");
            ModelState.Remove("Pet.Client");
            ModelState.Remove("Dependent.Client");
            ModelState.Remove("ClientMVA.Client");
            ModelState.Remove("InsuranceClaim.Client");
            ModelState.Remove("InsuranceClaim.InsuranceCompany");
            if (id == null)
            {
                ModelState.Remove("Pet.Id");
                ModelState.Remove("Dependent.Id");
                ModelState.Remove("ClientMVA.Id");
            }

            if (ModelState.IsValid)
            {
                if (id != null)
                {
                    _context.Entry(client).State = EntityState.Modified;
                    _context.Update(client.Pet!);
                    _context.Update(client.Dependent!);
                    _context.Update(client.ClientMVA!);
                    //_context.Update(client.HealthFile!);
                    //_context.Entry(client.Pet!).State = EntityState.Modified;
                    //_context.Entry(client.Dependent!).State = EntityState.Modified;
                    //_context.Entry(client.ClientMVA!).State = EntityState.Modified;
                    TempData["NotifyMsg"] = string.Format("Client Record Updated");
                }
                else
                {

                    client.Status ??= "Active"; 

                    _context.Add(client);

                    // We have to set the client of the children models, otherwise it will be null
                    client.Pet!.Client = client;
                    client.Dependent!.Client = client;
                    client.ClientMVA!.Client = client;

                    _context.Add(client.Pet);
                    _context.Add(client.Dependent);
                    _context.Add(client.ClientMVA);
                    TempData["NotifyMsg"] = string.Format("Client Record Added");
                }
                await _context.SaveChangesAsync();
                TempData["NotifyClassName"] = "success"; 
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        // GET: Client/Edit/5
        public async Task<IActionResult> CreateOrEdit(int? id, int? clientId)
        {

            Client client = new Client();
            if (id != null)
            {
                client = _context.Clients
               .Where(c => c.Id == id)
               .FirstOrDefault() ?? client;

                /*_context.Entry(client)
                    .Reference(c => c.ClientMVA)
                    .Load();
                _context.Entry(client)
                    .Reference(c => c.Dependent)
                    .Load();
                _context.Entry(client)
                    .Reference(c => c.Pet)
                    .Load();*/
                client.ClientMVA = await _context.ClientMVA
                .Where(cmva => cmva.Client.Id == id)
                .FirstOrDefaultAsync();
                client.Dependent = await _context.Dependent
                .Where(cdep => cdep.Client.Id == id)
                .FirstOrDefaultAsync();
                client.Pet = await _context.Pet
                .Where(cpet => cpet.Client.Id == id)
                .FirstOrDefaultAsync();
            }
            await _context.Clients.FindAsync(id);
            return View(client);

            /*if (id == null || _context.Clients == null)
              {
                  return NotFound();
              }

              var 
              if (client == null)
              {
                  return NotFound();
              }
              return View(client); */
        }


        // POST: Client/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Edit(int id, [Bind("Id, Pet,Dependent,ClientMVA,Referral,ContactName,Email,CellPhone,Telephone,Fax,Address,City,Region,Country,PostalCode,Status")] Client client)

        {
            if (id != client.Id)
            {
                return NotFound();
            }
            Console.WriteLine("Running line 64");
            if (ModelState.IsValid)
            {

                client.Status ??= "Active";

                try
                {
                    _context.Update(client);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClientExists(client.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
				return RedirectToAction("Details", "Client", new {id = id});   

				//return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        // GET: Client/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Clients == null)
            {
                return NotFound();
            }

            var client = await _context.Clients
                .FirstOrDefaultAsync(m => m.Id == id);
            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }

       private void CreateNotifyMsg(string notifyMsg, string notifyClassName)
        {
            TempData["NotifyMsg"] = notifyMsg;
            TempData["NotifyClassName"] = notifyClassName;
        }

        // POST: Client/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // if (_context.Clients == null)
            // {
            //     CreateNotifyMsg(string.Format("Failed to find Client ID={0}", id), "error");
            //     return new JsonResult(new { result = false });
            // }
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
            {

                return new JsonResult(new { result = false });
            }

            // await _context.SaveChangesAsync();
            // return RedirectToAction(nameof(Index));
            try{
                _context.Clients.Remove(client);
                await _context.SaveChangesAsync();
                TempData["NotifyClassName"] = "success"; 
                return new JsonResult(new { result = true });
            }
            catch(Exception ex)
            {
                //CreateNotifyMsg(ex.Message, "error");
                return new JsonResult(new { result = false });
            }
        }


        private bool ClientExists(int id)
        {
            return (_context.Clients?.Any(e => e.Id == id)).GetValueOrDefault();
        }


        
        public IActionResult ArchiveClient()
{
    var inactiveClients = _context.Clients.Where(c => c.Status == "Inactive").ToList();
    return View(inactiveClients);

}

// POST: Client/SetInactive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetInactive(int id)
        {
            try
            {
                var client = await _context.Clients.FindAsync(id);
                if (client == null)
                {
                    return NotFound();
                }

                client.Status = "Inactive";
                await _context.SaveChangesAsync();

                TempData["NotifyClassName"] = "success";
                TempData["NotifyMsg"] = "Client status set to Inactive successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["NotifyClassName"] = "danger";
                TempData["NotifyMsg"] = "An error occurred while setting the client status to Inactive.";

                return RedirectToAction(nameof(Index));
            }
        }




        //===================================== Download OCF Forms ========================================//

        //Helper method used to generate the OCF forms
        private byte[] GenerateOCF(Client client, ClientMVA clientMVA, InsuranceClaim insuranceClaim, string templatePath)
        {
            using (var templateStream = new FileStream(templatePath, FileMode.Open))
            {
                using (var memoryStream = new MemoryStream())
                {
                    var pdfReader = new PdfReader(templateStream);
                    var pdfWriter = new PdfWriter(memoryStream);
                    using (var pdfDocument = new PdfDocument(pdfReader, pdfWriter))
                    {
                        var form = PdfAcroForm.GetAcroForm(pdfDocument, true);

                        /*----------first and last name--------------*/


                        string[] nameParts = client.ContactName.Split(' ');
  


                        var firstNameField = form.GetField("FirstName");
                        if (firstNameField != null)
                        {

                            firstNameField.SetValue(nameParts[0]);

                        }

                        var firstNameAndInitialField = form.GetField("FirstNameAndInitial");
                        if (firstNameAndInitialField != null)
                        {

                            firstNameAndInitialField.SetValue(nameParts[0]);

                        }


                        var lastNameField = form.GetField("LastName");
                        if (lastNameField != null)
                        {

                            lastNameField.SetValue(nameParts[1]);

                        }


                        /*------------other fields----------------*/

                        var addressField = form.GetField("Address");
                        if (addressField != null)
                        {
                            addressField.SetValue(client.Address);
                        }

                        var emailField = form.GetField("PostalCode");
                        if (emailField != null)
                        {
                            emailField.SetValue(client.PostalCode);
                        }

                        var cityField = form.GetField("City");
                        if (cityField != null)
                        {
                            cityField.SetValue(client.City);
                        }

                        var provinceField = form.GetField("Province");
                        if (provinceField != null)
                        {
                            provinceField.SetValue(client.Region);
                        }


                        //?? serves as OR statements because some forms have different field names. (See Ryan's documentation or Docfly)
                        var cellPhoneField = form.GetField("WorkPhone") ?? form.GetField("Part 1 Work") ?? form.GetField("Text7");
                        if (cellPhoneField != null)
                        {
                            cellPhoneField.SetValue(client.CellPhone);
                        }

                        var telephoneField = form.GetField("HomeTelephone") ?? form.GetField("TelephoneNumber");
                        if (telephoneField != null)
                        {
                            telephoneField.SetValue(client.Telephone);
                        }

                        //?? serves as OR statements because some forms have different field names. (See Ryan's documentation or Docfly)
                        var dobField = form.GetField("DateOfBirth") ?? form.GetField("DateOfbirth") ?? form.GetField("BirthDate");
                        if (dobField != null)
                        {
                            dobField.SetValue(clientMVA.Dob.ToString("dd MMM yyyy"));
                        }


                        // Set gender-specific checkboxes
                        if (clientMVA.Gender == GenderEnum.Male)
                        {
                            var checkBoxMaleField = form.GetField("CheckBoxMale");
                            if (checkBoxMaleField != null)
                            {
                                checkBoxMaleField.SetValue(true.ToString());
                            }
                        }
                        else if (clientMVA.Gender == GenderEnum.Female)
                        {
                            var checkBoxFemaleField = form.GetField("CheckBoxFemale");
                            if (checkBoxFemaleField != null)
                            {
                                checkBoxFemaleField.SetValue(true.ToString());
                            }
                        }

                        //========= Set insurance company information ========//
                        
                        //Insurance Company Name
                        var insuranceCompanyField = form.GetField("NameOfInsuranceCompany");
                        if (insuranceCompanyField != null)
                        {
                            insuranceCompanyField.SetValue(insuranceClaim?.InsuranceCompany?.Title ?? "");
                        }

                        //Insurance Company Address
                        var insuranceCompanyAddressField = form.GetField("InsuranceAddress");
                        if (insuranceCompanyAddressField != null)
                        {
                            insuranceCompanyAddressField.SetValue(insuranceClaim?.InsuranceCompany?.Address ?? "");
                        }

                        //Insurance Region
                        var insuranceCompanyRegionField = form.GetField("InsuranceProvince");
                        if(insuranceCompanyRegionField != null)
                        {
                            var insuranceCompanyContact = insuranceClaim?.InsuranceCompany?.InsuranceCompanyContacts.FirstOrDefault();

                            insuranceCompanyRegionField.SetValue(insuranceCompanyContact?.Region);

                        }

                        //Insurance City
                        var insuranceCompanyCityField = form.GetField("InsuranceCity");
                        if (insuranceCompanyCityField != null)
                        {
                            var insuranceCompanyContact = insuranceClaim?.InsuranceCompany?.InsuranceCompanyContacts.FirstOrDefault();
                            
                            insuranceCompanyCityField.SetValue(insuranceCompanyContact?.City);

                        }

                        //Insurace Claim holder name
                        var insuranceClaimHolderNameField = form.GetField("NameOfPolicyholder");
                        if (insuranceClaimHolderNameField != null)
                        {
                            insuranceClaimHolderNameField.SetValue(client.ContactName);
                        }

                        // Insurance Claim Number
                        var insurancePolicyNumberField = form.GetField("InsurancePolicyNumber");
                        if(insurancePolicyNumberField != null)
                        {
                            insurancePolicyNumberField.SetValue(insuranceClaim.Claimref);
                        }





                        // Repeat for each field...

                        form.FlattenFields();
                    }

                    return memoryStream.ToArray();
                }
            }
        }
        private async Task<IActionResult> DownloadOCF(int? id, string templatePath, string formName)
        {
            if (id == null || _context.Clients == null)
            {
                return NotFound();
            }

            var client = await _context.Clients
                .FirstOrDefaultAsync(m => m.Id == id);

            if (client == null)
            {
                return NotFound();
            }

            // Retrieve clientMVA information
            var clientMVA = await _context.ClientMVA
                .Where(cmva => cmva.Client.Id == id)
                .FirstOrDefaultAsync();

            if (clientMVA == null)
            {
                return NotFound();
            }

            // Retrieve insurance claim information
            var insuranceClaim = await _context.InsuranceClaims
                .Where(ic => ic.Client.Id == id)
                .Include(ic => ic.InsuranceCompany)
                .FirstOrDefaultAsync();

            var filledPdfBytes = GenerateOCF(client, clientMVA, insuranceClaim, templatePath);

            // Append current date and time to the file name
            var currentDate = DateTime.Now.ToString("yyyy/MM/dd");
            var fileName = $"{client.ContactName}_{currentDate}_{formName}.pdf";

            return File(filledPdfBytes, "application/pdf", fileName);
        }


        // Install OCF-1
        public async Task<IActionResult> DownloadOCF1(int? id)
        {
            return await DownloadOCF(id, "Forms/OCF-1.pdf", "OCF-1");
        }

        // Install OCF-2
        public async Task<IActionResult> DownloadOCF2(int? id)
        {
            return await DownloadOCF(id, "Forms/OCF-2.pdf", "OCF-2");
        }

        // Install OCF-3
        public async Task<IActionResult> DownloadOCF3(int? id)
        {
            return await DownloadOCF(id, "Forms/OCF-3.pdf", "OCF-3");
        }

        // Install OCF-6
        public async Task<IActionResult> DownloadOCF6(int? id)
        {
            return await DownloadOCF(id, "Forms/OCF-6.pdf", "OCF-6");
        }

        // Install OCF-10
        public async Task<IActionResult> DownloadOCF10(int? id)
        {
            return await DownloadOCF(id, "Forms/OCF-10.pdf", "OCF-10");
        }



    }
}