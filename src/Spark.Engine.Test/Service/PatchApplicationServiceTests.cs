﻿namespace Spark.Engine.Test.Service
{
    using System.Linq;
    using System.Reflection;
    using Engine.Service.FhirServiceExtensions;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Serialization;
    using Xunit;
    using Task = System.Threading.Tasks.Task;

    public class PatchApplicationServiceTests
    {
        [Fact]
        public void CanApplyPropertyAssignmentPatch()
        {
            var xml = @"<Parameters xmlns=""http://hl7.org/fhir"">
  <parameter>
    <name value=""operation""/>
    <part>
      <name value=""type""/>
      <valueCode value=""add""/>
    </part>
    <part>
      <name value=""path""/>
      <valueString value=""Patient""/>
    </part>
    <part>
      <name value=""name""/>
      <valueString value=""birthDate""/>
    </part>
    <part>
      <name value=""value""/>
      <valueDate value=""1930-01-01""/>
    </part>
  </parameter>
</Parameters>";
            var parser = new FhirXmlParser();
            var parameters = parser.Parse<Parameters>(xml);

            var resource = new Patient { Id = "test" };
            var applier = new PatchApplicationService();
            resource = (Patient)applier.Apply(resource, parameters);

            Assert.Equal("1930-01-01", resource.BirthDate);
        }

        [Fact]
        public async System.Threading.Tasks.Task WhenApplyingPropertyAssignmentPatchToNonEmptyPropertyThenThrows()
        {
            var xml = @"<Parameters xmlns=""http://hl7.org/fhir"">
  <parameter>
    <name value=""operation""/>
    <part>
      <name value=""type""/>
      <valueCode value=""add""/>
    </part>
    <part>
      <name value=""path""/>
      <valueString value=""Patient""/>
    </part>
    <part>
      <name value=""name""/>
      <valueString value=""birthDate""/>
    </part>
    <part>
      <name value=""value""/>
      <valueDate value=""1930-01-01""/>
    </part>
  </parameter>
</Parameters>";
            var parser = new FhirXmlParser();
            var parameters = parser.Parse<Parameters>(xml);

            var resource = new Patient {Id = "test", BirthDate = "1930-01-01"};
            var applier = new PatchApplicationService();

            await Assert
                .ThrowsAsync<TargetInvocationException>(() => Task.Run(() => applier.Apply(resource, parameters)))
                .ConfigureAwait(false);
        }

        [Fact]
        public void CanApplyCollectionAddPatch()
        {
            var xml = @"<Parameters xmlns=""http://hl7.org/fhir"">
  <parameter>
    <name value=""operation""/>
    <part>
      <name value=""type""/>
      <valueCode value=""add""/>
    </part>
    <part>
      <name value=""path""/>
      <valueString value=""Patient""/>
    </part>
    <part>
      <name value=""name""/>
      <valueString value=""name""/>
    </part>
    <part>
      <name value=""value""/>
      <part>
        <name value=""name""/>
        <valueHumanName>
          <given value=""John""/>
          <family value=""Doe""/>
        </valueHumanName>
      </part>
    </part>
  </parameter>
</Parameters>";
            var parser = new FhirXmlParser();
            var parameters = parser.Parse<Parameters>(xml);

            var resource = new Patient { Id = "test" };
            var applier = new PatchApplicationService();
            resource = (Patient)applier.Apply(resource, parameters);

            Assert.Equal("John", resource.Name[0].Given.First());
            Assert.Equal("Doe", resource.Name[0].Family);
        }

        [Fact]
        public void CanApplyCollectionReplacePatch()
        {
            var xml = @"<Parameters xmlns=""http://hl7.org/fhir"">
  <parameter>
    <name value=""operation""/>
    <part>
      <name value=""type""/>
      <valueCode value=""replace""/>
    </part>
    <part>
      <name value=""path""/>
      <valueString value=""Patient.name[0]""/>
    </part>
    <part>
      <name value=""value""/>
      <part>
        <name value=""name""/>
        <valueHumanName>
          <given value=""Jane""/>
          <family value=""Doe""/>
        </valueHumanName>
      </part>
    </part>
  </parameter>
</Parameters>";
            var parser = new FhirXmlParser();
            var parameters = parser.Parse<Parameters>(xml);

            var resource = new Patient
            {
                Id = "test",
                Name = { new HumanName { Given = new[] { "John" }, Family = "Johnson" } }
            };
            var applier = new PatchApplicationService();
            resource = (Patient)applier.Apply(resource, parameters);

            Assert.Equal("Jane", resource.Name[0].Given.First());
            Assert.Equal("Doe", resource.Name[0].Family);
        }

        [Fact]
        public void CanApplyCollectionInsertPatch()
        {
            var xml = @"<Parameters xmlns=""http://hl7.org/fhir"">
  <parameter>
    <name value=""operation""/>
    <part>
      <name value=""type""/>
      <valueCode value=""insert""/>
    </part>
    <part>
      <name value=""path""/>
      <valueString value=""Patient.name[0]""/>
    </part>
    <part>
      <name value=""value""/>
      <part>
        <name value=""name""/>
        <valueHumanName>
          <given value=""Jane""/>
          <family value=""Doe""/>
        </valueHumanName>
      </part>
    </part>
  </parameter>
</Parameters>";
            var parser = new FhirXmlParser();
            var parameters = parser.Parse<Parameters>(xml);

            var resource = new Patient
            {
                Id = "test",
                Name = { new HumanName { Given = new[] { "John" }, Family = "Johnson" } }
            };
            var applier = new PatchApplicationService();
            resource = (Patient)applier.Apply(resource, parameters);

            Assert.Equal("Jane", resource.Name[0].Given.First());
            Assert.Equal("Doe", resource.Name[0].Family);

            Assert.Equal("John", resource.Name[1].Given.First());
            Assert.Equal("Johnson", resource.Name[1].Family);
        }

        [Fact]
        public void CanApplyCollectionMovePatch()
        {
            var xml = @"<Parameters xmlns=""http://hl7.org/fhir"">
  <parameter>
    <name value=""operation""/>
    <part>
      <name value=""type""/>
      <valueCode value=""move""/>
    </part>
    <part>
      <name value=""path""/>
      <valueString value=""Patient.name""/>
    </part>
    <part>
      <name value=""source""/>
      <valueString value=""1""/>
    </part>
    <part>
      <name value=""destination""/>
      <valueString value=""0""/>
    </part>
  </parameter>
</Parameters>";
            var parser = new FhirXmlParser();
            var parameters = parser.Parse<Parameters>(xml);

            var resource = new Patient
            {
                Id = "test",
                Name =
                {
                    new HumanName {Given = new[] {"John"}, Family = "Johnson"},
                    new HumanName {Given = new[] {"Jane"}, Family = "Doe"}
                }
            };
            var applier = new PatchApplicationService();
            resource = (Patient)applier.Apply(resource, parameters);

            Assert.Equal("Jane", resource.Name[0].Given.First());
            Assert.Equal("Doe", resource.Name[0].Family);

            Assert.Equal("John", resource.Name[1].Given.First());
            Assert.Equal("Johnson", resource.Name[1].Family);
        }

        [Fact]
        public void CanApplyPropertyReplacementPatch()
        {
            var xml = @"<Parameters xmlns=""http://hl7.org/fhir"">
  <parameter>
    <name value=""operation""/>
    <part>
      <name value=""type""/>
      <valueCode value=""replace""/>
    </part>
    <part>
      <name value=""path""/>
      <valueString value=""Patient.birthDate""/>
    </part>
    <part>
      <name value=""value""/>
      <valueDate value=""1930-01-01""/>
    </part>
  </parameter>
</Parameters>";
            var parser = new FhirXmlParser();
            var parameters = parser.Parse<Parameters>(xml);

            var resource = new Patient { Id = "test", BirthDate = "1970-12-24" };
            var applier = new PatchApplicationService();
            resource = (Patient)applier.Apply(resource, parameters);

            Assert.Equal("1930-01-01", resource.BirthDate);
        }

        [Fact]
        public void CanApplyCollectionDeletePatch()
        {
            var xml = @"<Parameters xmlns=""http://hl7.org/fhir"">
  <parameter>
    <name value=""operation""/>
    <part>
      <name value=""type""/>
      <valueCode value=""delete""/>
    </part>
    <part>
      <name value=""path""/>
      <valueString value=""Patient.name[0]""/>
    </part>
  </parameter>
</Parameters>";
            var parser = new FhirXmlParser();
            var parameters = parser.Parse<Parameters>(xml);

            var resource = new Patient { Id = "test", Name = { new HumanName { Text = "John Doe" } } };
            var applier = new PatchApplicationService();
            resource = (Patient)applier.Apply(resource, parameters);

            Assert.Empty(resource.Name);
        }
    }
}