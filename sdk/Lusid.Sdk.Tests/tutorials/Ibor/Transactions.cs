using System;
using System.Collections.Generic;
using System.Linq;
using Lusid.Sdk.Api;
using Lusid.Sdk.Model;
using Lusid.Sdk.Tests.Utilities;
using Lusid.Sdk.Utilities;
using NUnit.Framework;

using static Lusid.Sdk.Model.TransactionConfigurationMovementDataRequest;

namespace Lusid.Sdk.Tests.Tutorials.Ibor
{
    [TestFixture]
    public class Transactions
    {
        private IInstrumentsApi _instrumentsApi;
        private ITransactionPortfoliosApi _transactionPortfoliosApi;
        private IList<string> _instrumentIds;
        private ISystemConfigurationApi _systemConfigurationApi;
        
        private TestDataUtilities _testDataUtilities;

        [OneTimeSetUp]
        public void SetUp()
        {
            var apiFactory = LusidApiFactoryBuilder.Build("secrets.json");

            _instrumentsApi = apiFactory.Api<IInstrumentsApi>();
            _transactionPortfoliosApi = apiFactory.Api<ITransactionPortfoliosApi>();
            
            var instrumentLoader = new InstrumentLoader(apiFactory);
            _instrumentIds = instrumentLoader.LoadInstruments();
            _testDataUtilities = new TestDataUtilities(apiFactory.Api<ITransactionPortfoliosApi>());

            _systemConfigurationApi = apiFactory.Api<ISystemConfigurationApi>();
        }

        [Test]
        public void List_Set_And_Create_Transaction_Types()
        {
            //    first, let's try using the API to see what it already knows about transaction types
            var @default = _systemConfigurationApi.ListConfigurationTransactionTypes();

            //    it should know about a single, simple Buy transaction
            var buy = @default.TransactionConfigs.Single(c => c.Aliases.Any(a => a.Type == "Buy"));
            
            //    now let's create an alternative Buy, and add it to the default configuration
            //    (for reference -- this isn't a complete, sensible transaction definition)
            var newBuy = new TransactionConfigurationDataRequest(
                aliases: new List<TransactionConfigurationTypeAlias> 
                    { new TransactionConfigurationTypeAlias("NewBuy", "Description", "Class", "Group", 0) },
                movements: new List<TransactionConfigurationMovementDataRequest>()
                    { new TransactionConfigurationMovementDataRequest(MovementTypesEnum.Commitment, SideEnum.Side1, 1, null, null) }
            );

            _systemConfigurationApi.CreateConfigurationTransactionType(newBuy);
            
            //    there should be one more entry in the updated configuration
            var updated = _systemConfigurationApi.ListConfigurationTransactionTypes();
            Assert.That(updated.TransactionConfigs.Count() - @default.TransactionConfigs.Count(), Is.EqualTo(1));
            
            //    now let's use our Buy, and replace the default configuration
            //    REALISE THAT THIS IS A DESTRUCTIVE OPERATION -- you can't trivially recover the original defaults
            //    unless you have them stored down -- we'll restore them afterwards
            var request = new TransactionSetConfigurationDataRequest(new [] { newBuy }.ToList());
            
            _systemConfigurationApi.SetConfigurationTransactionTypes(request);
            
            updated = _systemConfigurationApi.ListConfigurationTransactionTypes();
            
            //    there should be one entry in the updated configuration
            Assert.That(updated.TransactionConfigs.Count(), Is.EqualTo(1));
            
            //    finally, let's restore our original default configurations
            var defaultRequest = @default.ConvertToRequest();
            
            _systemConfigurationApi.SetConfigurationTransactionTypes(defaultRequest);
            
            updated = _systemConfigurationApi.ListConfigurationTransactionTypes();
            
            //    and confirm that the count of transaction configurations is the same
            Assert.That(updated.TransactionConfigs.Count(), Is.EqualTo(@default.TransactionConfigs.Count()));
        }
        
        [Test]
        public void Load_Listed_Instrument_Transaction()
        {
            var effectiveDate = new DateTimeOffset(2018, 1, 1, 0, 0, 0, TimeSpan.Zero);
            
            //    create the portfolio
            var portfolioCode = _testDataUtilities.CreateTransactionPortfolio(TestDataUtilities.TutorialScope);
            
            //    create the transaction request
            var transaction = new TransactionRequest(
                
                //    unique transaction id
                transactionId: Guid.NewGuid().ToString(),
                
                //    instruments must already exist in LUSID and have a valid LUSID instrument id
                instrumentIdentifiers: new Dictionary<string, string>
                {
                    [TestDataUtilities.LusidInstrumentIdentifier] = _instrumentIds[0]
                },
                
                type: "Buy",
                totalConsideration: new CurrencyAndAmount(1230, "GBP"),
                transactionDate: effectiveDate,
                settlementDate: effectiveDate,
                units: 100,
                transactionPrice: new TransactionPrice(12.3),
                source: "Custodian");
            
            //    add the transaction
            _transactionPortfoliosApi.UpsertTransactions(TestDataUtilities.TutorialScope, portfolioCode, new List<TransactionRequest> {transaction});
            
            //    get the transaction
            var transactions = _transactionPortfoliosApi.GetTransactions(TestDataUtilities.TutorialScope, portfolioCode);
            
            Assert.That(transactions.Values, Has.Count.EqualTo(1));
            Assert.That(transactions.Values[0].TransactionId, Is.EqualTo(transaction.TransactionId));
        }
        
        [Test]
        public void Load_Cash_Transaction()
        {
            var effectiveDate = new DateTimeOffset(2018, 1, 1, 0, 0, 0, TimeSpan.Zero);
            
            //    create the portfolio
            var portfolioCode = _testDataUtilities.CreateTransactionPortfolio(TestDataUtilities.TutorialScope);
            
            //    create the transaction request
            var transaction = new TransactionRequest(
                
                //    unique transaction id
                transactionId: Guid.NewGuid().ToString(),
                
                //    instruments must already exist in LUSID and have a valid LUSID instrument id
                instrumentIdentifiers: new Dictionary<string, string>
                {
                    [TestDataUtilities.LusidCashIdentifier] = "GBP"
                },
                
                type: "FundsIn",
                totalConsideration: new CurrencyAndAmount(0.0, "GBP"),
                transactionPrice: new TransactionPrice(0.0),
                transactionDate: effectiveDate,
                settlementDate: effectiveDate,
                units: 100,
                source: "Custodian");
            
            //    add the transaction
            _transactionPortfoliosApi.UpsertTransactions(TestDataUtilities.TutorialScope, portfolioCode, new List<TransactionRequest> {transaction});
            
            //    get the transaction
            var transactions = _transactionPortfoliosApi.GetTransactions(TestDataUtilities.TutorialScope, portfolioCode);
            
            Assert.That(transactions.Values, Has.Count.EqualTo(1));
            Assert.That(transactions.Values[0].TransactionId, Is.EqualTo(transaction.TransactionId));
        }
        
        [Test]
        public void Load_Otc_Instrument_Transaction()
        {
            var effectiveDate = new DateTimeOffset(2018, 1, 1, 0, 0, 0, TimeSpan.Zero);
            
            //    create the portfolio
            var portfolioCode = _testDataUtilities.CreateTransactionPortfolio(TestDataUtilities.TutorialScope);
            
            //    swap definition, this is uploaded in a client custom format
            var swapDefinition = new InstrumentDefinition(
                name: "10mm 5Y Fixed",
                identifiers: new Dictionary<string, InstrumentIdValue>
                {
                    ["ClientInternal"] = new InstrumentIdValue(value: "SW-1")
                },
                definition: new InstrumentEconomicDefinition(
                    instrumentFormat: "CustomFormat",
                    content: "<customFormat>upload in custom xml or JSON format</customFormat>"
                ));
            
            //    create the swap
            var createSwapResponse = _instrumentsApi.UpsertInstruments(new Dictionary<string, InstrumentDefinition>
            {
                ["correlationId"] = swapDefinition
            });

            var swapId = createSwapResponse.Values.Values.Select(i => i.LusidInstrumentId).FirstOrDefault();
            
            //    create the transaction request
            var transaction = new TransactionRequest(
                
                //    unique transaction id
                transactionId: Guid.NewGuid().ToString(),
                
                //    instruments must already exist in LUSID and have a valid LUSID instrument id
                instrumentIdentifiers: new Dictionary<string, string>
                {
                    [TestDataUtilities.LusidInstrumentIdentifier] = swapId
                },
                
                type: "Buy",
                totalConsideration: new CurrencyAndAmount(0.0, "GBP"),
                transactionPrice: new TransactionPrice(0.0),
                transactionDate: effectiveDate,
                settlementDate: effectiveDate,
                units: 1,
                source: "Custodian");
            
            //    add the transaction
            _transactionPortfoliosApi.UpsertTransactions(TestDataUtilities.TutorialScope, portfolioCode, new List<TransactionRequest> {transaction});
            
            //    get the transaction
            var transactions = _transactionPortfoliosApi.GetTransactions(TestDataUtilities.TutorialScope, portfolioCode);
            
            Assert.That(transactions.Values, Has.Count.EqualTo(1));
            Assert.That(transactions.Values[0].TransactionId, Is.EqualTo(transaction.TransactionId));
            Assert.That(transactions.Values[0].InstrumentUid, Is.EqualTo(swapId));
        }

        [Test]
        public void Cancel_Transactions()
        {
            var effectiveDate = new DateTimeOffset(2018, 1, 1, 0, 0, 0, TimeSpan.Zero);
            
            //    create the portfolio
            var portfolioCode = _testDataUtilities.CreateTransactionPortfolio(TestDataUtilities.TutorialScope);
            
            //    create the transaction requests
            var transactionRequests = new[]
            {
                new TransactionRequest(

                    //    unique transaction id
                    transactionId: Guid.NewGuid().ToString(),

                    //    instruments must already exist in LUSID and have a valid LUSID instrument id
                    instrumentIdentifiers: new Dictionary<string, string>
                    {
                        [TestDataUtilities.LusidInstrumentIdentifier] = _instrumentIds[0]
                    },

                    type: "Buy",
                    totalConsideration: new CurrencyAndAmount(1230, "GBP"),
                    transactionDate: effectiveDate,
                    settlementDate: effectiveDate,
                    units: 100,
                    transactionPrice: new TransactionPrice(12.3),
                    source: "Custodian"),
                new TransactionRequest(

                    //    unique transaction id
                    transactionId: Guid.NewGuid().ToString(),

                    //    instruments must already exist in LUSID and have a valid LUSID instrument id
                    instrumentIdentifiers: new Dictionary<string, string>
                    {
                        [TestDataUtilities.LusidInstrumentIdentifier] = _instrumentIds[0]
                    },

                    type: "Sell",
                    totalConsideration: new CurrencyAndAmount(45, "GBP"),
                    transactionDate: effectiveDate,
                    settlementDate: effectiveDate,
                    units: 50,
                    transactionPrice: new TransactionPrice(20.4),
                    source: "Custodian")
            };

            //    add the transactions
            _transactionPortfoliosApi.UpsertTransactions(TestDataUtilities.TutorialScope, portfolioCode, transactionRequests.ToList());
            
            //    get the transactions
            var transactions = _transactionPortfoliosApi.GetTransactions(TestDataUtilities.TutorialScope, portfolioCode);
            
            Assert.That(transactions.Values, Has.Count.EqualTo(2));
            Assert.That(transactions.Values.Select(t => t.TransactionId), Is.EquivalentTo(transactionRequests.Select(t => t.TransactionId)));

            //    cancel the transactions
            _transactionPortfoliosApi.CancelTransactions(TestDataUtilities.TutorialScope, portfolioCode, transactions.Values.Select(t => t.TransactionId).ToList());

            //    verify the portfolio is now empty
            var noTransactions = _transactionPortfoliosApi.GetTransactions(TestDataUtilities.TutorialScope, portfolioCode);

            Assert.That(noTransactions.Values, Is.Empty);
        }
    }
}