using System.IO;
using Dicom;
using DicomAnnotationsDrawing.Models;

namespace DicomAnnotationsDrawing.Services
{
    public static class SrExporter
    {
        public static string ExportTID1500(IEnumerable<Annotation> annotations, DicomDataset imageDs, string? outputPath = null)
        {
            ArgumentNullException.ThrowIfNull(imageDs);
            ArgumentNullException.ThrowIfNull(annotations);

            var sopInstanceUID = imageDs.GetSingleValue<string>(DicomTag.SOPInstanceUID);
            var sopClassUID = imageDs.GetSingleValue<string>(DicomTag.SOPClassUID);
            var studyUID = imageDs.GetSingleValue<string>(DicomTag.StudyInstanceUID);

            // Root SR dataset
            var sr = new DicomDataset
            {
                { DicomTag.SOPClassUID, DicomUID.ComprehensiveSRStorage },
                { DicomTag.SOPInstanceUID, DicomUID.Generate() },
                { DicomTag.StudyInstanceUID, studyUID },
                { DicomTag.SeriesInstanceUID, DicomUID.Generate() },
                { DicomTag.Modality, "SR" },
                { DicomTag.PatientName, imageDs.GetSingleValueOrDefault(DicomTag.PatientName, "") },
                { DicomTag.PatientID, imageDs.GetSingleValueOrDefault(DicomTag.PatientID, "") },
                { DicomTag.PatientSex, imageDs.GetSingleValueOrDefault(DicomTag.PatientSex, "") },
                { DicomTag.StudyID, imageDs.GetSingleValueOrDefault(DicomTag.StudyID, "") },
                { DicomTag.StudyDate, imageDs.GetSingleValueOrDefault(DicomTag.StudyDate, "") },
                { DicomTag.StudyTime, imageDs.GetSingleValueOrDefault(DicomTag.StudyTime, "") },
                { DicomTag.SeriesDate, DateTime.UtcNow },
                { DicomTag.SeriesTime, DateTime.UtcNow },
                { DicomTag.ContentDate, DateTime.UtcNow },
                { DicomTag.ContentTime, DateTime.UtcNow },
                { DicomTag.SeriesNumber, "1" },
                { DicomTag.InstanceNumber, "1" },
                { DicomTag.InstanceCreationDate, DateTime.UtcNow.ToString("yyyyMMdd") },
                { DicomTag.InstanceCreationTime, DateTime.UtcNow.ToString("HHmmss") },
                { DicomTag.SpecificCharacterSet, "ISO_IR 100" },
                { DicomTag.ValueType, "CONTAINER" },
                { DicomTag.ContinuityOfContent, "CONTINUOUS" }
            };

            sr.Add(new DicomSequence(DicomTag.ConceptNameCodeSequence,
                new DicomDataset {
                    { DicomTag.CodeValue, "126000" },
                    { DicomTag.CodingSchemeDesignator, "DCM" },
                    { DicomTag.CodeMeaning, "Imaging Measurement Report" }
                }));

            sr.Add(new DicomSequence(DicomTag.ContentTemplateSequence,
                new DicomDataset {
                    { DicomTag.MappingResource, "DCMR" },
                    { DicomTag.TemplateIdentifier, "1500" }
                }));

            // Measurements container
            var measContainer = new DicomDataset
            {
                { DicomTag.ValueType, "CONTAINER" },
                { DicomTag.RelationshipType, "CONTAINS" },
                { DicomTag.ContinuityOfContent, "CONTINUOUS" }
            };
            measContainer.Add(new DicomSequence(DicomTag.ConceptNameCodeSequence,
                new DicomDataset {
                    { DicomTag.CodeValue, "126010" },
                    { DicomTag.CodingSchemeDesignator, "DCM" },
                    { DicomTag.CodeMeaning, "Imaging Measurements" }
                }));

            var measurementGroups = new List<DicomDataset>();

            foreach (var ann in annotations)
            {
                var group = new DicomDataset
                {
                    { DicomTag.ValueType, "CONTAINER" },
                    { DicomTag.RelationshipType, "CONTAINS" },
                    { DicomTag.ContinuityOfContent, "CONTINUOUS" }
                };

                group.Add(new DicomSequence(DicomTag.ConceptNameCodeSequence,
                    new DicomDataset {
                        { DicomTag.CodeValue, "125007" },
                        { DicomTag.CodingSchemeDesignator, "DCM" },
                        { DicomTag.CodeMeaning, "Measurement Group" }
                    }));

                group.Add(new DicomSequence(DicomTag.ContentTemplateSequence,
                    new DicomDataset {
                        { DicomTag.MappingResource, "DCMR" },
                        { DicomTag.TemplateIdentifier, "1410" }
                    }));

                // Tracking Identifier
                var trackingId = new DicomDataset
                {
                    { DicomTag.ValueType, "TEXT" },
                    { DicomTag.RelationshipType, "HAS OBS CONTEXT" },
                    { DicomTag.TextValue, $"Annotation {ann.Id}" }
                };
                trackingId.Add(new DicomSequence(DicomTag.ConceptNameCodeSequence,
                    new DicomDataset {
                        { DicomTag.CodeValue, "112039" },
                        { DicomTag.CodingSchemeDesignator, "DCM" },
                        { DicomTag.CodeMeaning, "Tracking Identifier" }
                    }));

                // Tracking UID
                var trackingUid = new DicomDataset
                {
                    { DicomTag.ValueType, "UIDREF" },
                    { DicomTag.RelationshipType, "HAS OBS CONTEXT" },
                    { DicomTag.UID, DicomUID.Generate().UID }
                };
                trackingUid.Add(new DicomSequence(DicomTag.ConceptNameCodeSequence,
                    new DicomDataset {
                        { DicomTag.CodeValue, "112040" },
                        { DicomTag.CodingSchemeDesignator, "DCM" },
                        { DicomTag.CodeMeaning, "Tracking Unique Identifier" }
                    }));

                // Geometry (SCOORD)
                var scoord = new DicomDataset
                {
                    { DicomTag.ValueType, "SCOORD" },
                    { DicomTag.RelationshipType, "CONTAINS" },
                    { DicomTag.GraphicType, "POLYLINE" },
                    {
                        DicomTag.GraphicData,
                        new[]
                        {
                            ann.X, ann.Y,
                            ann.X + ann.Width, ann.Y,
                            ann.X + ann.Width, ann.Y + ann.Height,
                            ann.X, ann.Y + ann.Height,
                            ann.X, ann.Y
                        }
                    }
                };
                scoord.Add(new DicomSequence(DicomTag.ConceptNameCodeSequence,
                    new DicomDataset {
                        { DicomTag.CodeValue, "111030" },
                        { DicomTag.CodingSchemeDesignator, "DCM" },
                        { DicomTag.CodeMeaning, "Image Region" }
                    }));
                scoord.Add(new DicomSequence(DicomTag.ContentSequence,
                    new DicomDataset
                    {
                        { DicomTag.RelationshipType, "SELECTED FROM" },
                        { DicomTag.ValueType, "IMAGE" },
                        new DicomSequence(DicomTag.ReferencedSOPSequence,
                            new DicomDataset
                            {
                                { DicomTag.ReferencedSOPClassUID, sopClassUID },
                                { DicomTag.ReferencedSOPInstanceUID, sopInstanceUID }
                            }),
                        new DicomSequence(DicomTag.ConceptNameCodeSequence,
                            new DicomDataset
                            {
                                { DicomTag.CodeValue, "111040" },
                                { DicomTag.CodingSchemeDesignator, "DCM" },
                                { DicomTag.CodeMeaning, "Original Source" }
                            })
                    }));

                group.Add(new DicomSequence(DicomTag.ContentSequence, trackingId, trackingUid, scoord));
                measurementGroups.Add(group);
            }

            measContainer.Add(new DicomSequence(DicomTag.ContentSequence, measurementGroups.ToArray()));
            sr.Add(new DicomSequence(DicomTag.ContentSequence, measContainer));

            var srFile = new DicomFile(sr);
            var savePath = outputPath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "TID1500_Annotations_SR.dcm");

            srFile.Save(savePath);
            return savePath;
        }
    }
}
