using CarSlineAPI.Models.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CarSlineAPI.Services
{
    public interface IPdfService
    {
        Task<byte[]> GenerarPdfOrdenAsync(OrdenPdfDto orden);
        Task<byte[]> GuardarPdfOrdenAsync(OrdenPdfDto orden, string numeroOrden);
    }

    public class PdfService : IPdfService
    {
        private readonly ILogger<PdfService> _logger;
        private readonly string _rutaBasePdfs = @"C:\Users\LENOVO\Downloads\Evidencias_Ordenes\";

        public PdfService(ILogger<PdfService> logger)
        {
            _logger = logger;
            
            // Configurar licencia de QuestPDF (Community - gratis)
            QuestPDF.Settings.License = LicenseType.Community;

            if (!Directory.Exists(_rutaBasePdfs))
            {
                Directory.CreateDirectory(_rutaBasePdfs);
            }
        }

        public async Task<byte[]> GenerarPdfOrdenAsync(OrdenPdfDto orden)
        {
            try
            {
                _logger.LogInformation($"📄 Generando PDF para orden {orden.NumeroOrden}");

                var pdf = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.Letter);
                        page.Margin(40);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                        // Header
                        page.Header().Element(c => CrearEncabezado(c, orden));

                        // Content
                        page.Content().Element(c => CrearContenido(c, orden));

                        // Footer
                        page.Footer().Element(c => CrearPiePagina(c, orden));
                    });
                });

                var pdfBytes = pdf.GeneratePdf();
                _logger.LogInformation($"✅ PDF generado exitosamente: {pdfBytes.Length} bytes");

                return await Task.FromResult(pdfBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error al generar PDF para orden {orden.NumeroOrden}");
                throw;
            }
        }
        
        public async Task<byte[]> GuardarPdfOrdenAsync(OrdenPdfDto orden, string numeroOrden)
        {
            try
            {
                // Crear carpeta específica para la orden
                //string carpetaOrden = Path.Combine(_rutaBasePdfs, numeroOrden);
                //if (!Directory.Exists(carpetaOrden))
                //{
                // Directory.CreateDirectory(carpetaOrden);
                //}

                // Generar nombre del archivo
                //string nombreArchivo = $"{numeroOrden}/{numeroOrden}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                //string rutaCompleta = Path.Combine(carpetaOrden, nombreArchivo);

                // Generar y guardar PDF
                var pdfBytes = await GenerarPdfOrdenAsync(orden);
                //await File.WriteAllBytesAsync(rutaCompleta, pdfBytes);

                //_logger.LogInformation($"✅ PDF guardado en: {rutaCompleta}");

                return await Task.FromResult(pdfBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error al guardar PDF para orden {numeroOrden}");
                throw;
            }
        }
        
        // ============================================
        // MÉTODOS PRIVADOS DE CONSTRUCCIÓN DEL PDF
        // ============================================

        private void CrearEncabezado(IContainer container, OrdenPdfDto orden)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "logo.png");
                    // Logo del Taller
                    row.RelativeItem().Column(col =>
                    {
                        col.Item()
                            .Height(45)
                            .Image(logoPath)
                            .FitArea();

                        col.Item().Text("📍 Las Palomas 590, El Portezuelo")
                            .FontSize(10).Italic();
                        col.Item().Text("Tel: 771-295-4232")
                            .FontSize(9);
                    });

                    // Información de la Orden
                    row.ConstantItem(200).Column(col =>
                    {
                        col.Item().Background(Colors.Red.Darken2).Padding(8).Column(c =>
                        {
                            c.Item().Text(orden.NumeroOrden)
                                .FontSize(16).Bold().FontColor(Colors.White);
                            c.Item().Text(orden.TipoOrden)
                                .FontSize(11).FontColor(Colors.White);
                        });

                        col.Item().Padding(5).Column(c =>
                        {
                            c.Item().Text($"Estado de Orden: {orden.EstadoOrden}")
                                .FontSize(9).Bold();
                        });
                    });
                });

                column.Item().PaddingTop(10).LineHorizontal(2).LineColor(Colors.Red.Darken2);
            });
        }

        private void CrearContenido(IContainer container, OrdenPdfDto orden)
        {
            container.PaddingTop(15).Column(column =>
            {
                // Información del Cliente y Vehículo
                column.Item().Element(c => SeccionClienteVehiculo(c, orden));
                column.Item().PaddingTop(10);

                // Trabajos Realizados
                column.Item().PaddingTop(15).Element(c => SeccionTrabajos(c, orden));

                // Observaciones
                if (!string.IsNullOrWhiteSpace(orden.ObservacionesAsesor) ||
                    !string.IsNullOrWhiteSpace(orden.ObservacionesJefeTaller))
                {
                    column.Item().PaddingTop(15).Element(c => SeccionObservaciones(c, orden));
                }

                // Resumen de Costos
                column.Item().PaddingTop(15).Element(c => SeccionCostos(c, orden));


                // Checklist (si existe)
                if (orden.CheckList != null)
                {
                    column.Item().PaddingTop(15).PageBreak();
                    column.Item().Element(c => SeccionCheckList(c, orden.CheckList));
                }

            });
        }

        private void SeccionClienteVehiculo(IContainer container, OrdenPdfDto orden)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    // Cliente
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(10).Column(col =>
                        {
                            col.Item().Text("CLIENTE").FontSize(12).Bold()
                                .FontColor(Colors.Red.Darken2);
                            col.Item().PaddingTop(5).Text(orden.Cliente.NombreCompleto)
                                .FontSize(11).Bold();
                            col.Item().PaddingTop(2).Text($"RFC: {orden.Cliente.RFC}").FontSize(9);
                            col.Item().Text($"Tel: {orden.Cliente.TelefonoMovil}").FontSize(9);
                            if (!string.IsNullOrEmpty(orden.Cliente.CorreoElectronico))
                            {
                                col.Item().Text($"Email: {orden.Cliente.CorreoElectronico}")
                                    .FontSize(9);
                            }
                        });

                    row.ConstantItem(20);

                    // Vehículo
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(10).Column(col =>
                        {
                            col.Item().Text("VEHÍCULO").FontSize(12).Bold()
                                .FontColor(Colors.Red.Darken2);
                            col.Item().PaddingTop(5).Text(orden.Vehiculo.VehiculoCompleto)
                                .FontSize(11).Bold();
                            col.Item().PaddingTop(2).Text($"VIN: {orden.Vehiculo.VIN}").FontSize(9);
                            if (!string.IsNullOrEmpty(orden.Vehiculo.Placas))
                            {
                                col.Item().Text($"Placas: {orden.Vehiculo.Placas}")
                                    .FontSize(9);
                            }
                            col.Item().Text($"Kilometraje: {orden.Vehiculo.KilometrajeActual:N0} km")
                                .FontSize(9).Bold();
                        });
                });
            });
        }

        private void SeccionTrabajos(IContainer container, OrdenPdfDto orden)
        {
            container.Column(column =>
            {
                column.Item().Background(Colors.Red.Darken2).Padding(8)
                    .Text("TRABAJOS REALIZADOS").FontSize(13).Bold()
                    .FontColor(Colors.White);

                column.Item().PaddingTop(10);

                foreach (var trabajo in orden.Trabajos)
                {
                    column.Item().PaddingBottom(10).Border(1).EnsureSpace(200)
                        .BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
                        {
                        // Header del trabajo
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text(trabajo.Trabajo)
                                .FontSize(11).Bold();
                            row.ConstantItem(100).AlignRight()
                                .Text(trabajo.EstadoTrabajo)
                                .FontSize(9).Bold().FontColor(Colors.Green.Darken2);
                        });

                        if (!string.IsNullOrEmpty(trabajo.TecnicoNombre))
                        {
                            col.Item().PaddingTop(5).Text($"Técnico: {trabajo.TecnicoNombre}")
                                .FontSize(9).Italic();
                        }


                        if (trabajo.Refacciones != null && trabajo.Refacciones.Any())

                        {
                            col.Item().PaddingTop(8).Column(c =>
                            {
                                c.Item().Text("Refacciones:").FontSize(10).Bold();

                                c.Item().PaddingTop(5).Table(table =>
                                {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.ConstantColumn(60);
                                    columns.ConstantColumn(80);
                                    columns.ConstantColumn(80);
                                });

                                // Header
                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten3)
                                        .Padding(5).Text("Refacción").FontSize(9).Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3)
                                        .Padding(5).Text("Cant.").FontSize(9).Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3)
                                        .Padding(5).Text("P. Unit.").FontSize(9).Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3)
                                        .Padding(5).Text("Total").FontSize(9).Bold();
                                });

                                // Rows
                                foreach (var refaccion in trabajo.Refacciones)
                                    {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(5).Text(refaccion.Refaccion).FontSize(8);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(5).Text(refaccion.Cantidad.ToString()).FontSize(8);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(5).Text($"${refaccion.PrecioUnitario:N2}").FontSize(8);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(5).Text($"${refaccion.Total:N2}").FontSize(8).Bold();
                                }
                            });
                        });
                }

                // Costos del trabajo
                col.Item().PaddingTop(8).EnsureSpace(200).Row(row =>
                {
                    row.RelativeItem();
                    row.ConstantItem(250).Column(c =>
                    {
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Mano de Obra:");
                            r.ConstantItem(80).AlignRight()
                                .Text($"${trabajo.CostoManoObra:N2}").Bold();
                        });
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Refacciones:");
                            r.ConstantItem(80).AlignRight()
                                .Text($"${trabajo.TotalRefacciones:N2}").Bold();
                        });
                    });
                });

                // Comentarios del técnico
                if (!string.IsNullOrEmpty(trabajo.ComentariosTecnico))
                {
                    col.Item().PaddingTop(8).Background(Colors.Blue.Lighten4)
                        .Padding(8).Column(c =>
                        {
                            c.Item().Text("Comentarios del Técnico:")
                                        .FontSize(9).Bold();
                            c.Item().Text(trabajo.ComentariosTecnico)
                                        .FontSize(9);
                        });
                }
            });
        }
    });
}

        private void SeccionCheckList(IContainer container, CheckListPdfDto checkList)
        {
            container.Column(column =>
            {
                column.Item().Background(Colors.Red.Darken2).Padding(8)
                    .Text("CHECKLIST DE SERVICIO").FontSize(13).Bold()
                    .FontColor(Colors.White);

                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                    });

                    // Sistema de Dirección
                    AgregarSeccionCheckList(table, "SISTEMA DE DIRECCIÓN",
                        ("Bieletas", checkList.Bieletas),
                        ("Terminales de Direccion ", checkList.Terminales),
                        ("Caja de Dirección", checkList.CajaDireccion),
                        ("Volante", checkList.Volante));

                    // Sistema de Suspensión
                    AgregarSeccionCheckList(table, "SISTEMA DE SUSPENSIÓN",
                        ("Amortiguadores Delanteros", checkList.AmortiguadoresDelanteros),
                        ("Barra Estabilizadora", checkList.BarraEstabilizadora),
                        ("Amortiguadores Traseros", checkList.AmortiguadoresTraseros),
                        ("Horquillas", checkList.Horquillas));

                    // Neumáticos
                    AgregarSeccionCheckList(table, "NEUMÁTICOS",
                        ("Delanteros", checkList.NeumaticosDelanteros),
                        ("Balanceo", checkList.Balanceo),
                        ("Traseros", checkList.NeumaticosTraseros),
                        ("Alineación", checkList.Alineacion));

                    // Luces
                    AgregarSeccionCheckList(table, "LUCES",
                        ("Altas", checkList.LucesAltas),
                        ("Bajas", checkList.LucesBajas),
                        ("Antiniebla", checkList.LucesAntiniebla),
                        ("Reversa", checkList.LucesReversa),
                        ("Direccionales", checkList.LucesDireccionales),
                        ("Intermitentes", checkList.LucesIntermitentes));

                    // Sistema de Frenos
                    AgregarSeccionCheckList(table, "SISTEMA DE FRENOS",
                        ("Discos/Tambores Delanteros", checkList.DiscosTamboresDelanteros),
                        ("Balatas Delanteras", checkList.BalatasDelanteras),
                        ("Discos/Tambores Traseros", checkList.DiscosTamboresTraseros),
                        ("Balatas Traseras", checkList.BalatasTraseras));
                });

                // Piezas Reemplazadas y Trabajos
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).Column(col =>
                        {
                            col.Item().Background(Colors.Red.Darken2).Padding(3).Text("PIEZAS REEMPLAZADAS").FontSize(10).Bold().FontColor(Colors.White);
                            AgregarCheckItem(col, "Aceite de Motor", checkList.ReemplazoAceiteMotor);
                            AgregarCheckItem(col, "Filtro Aceite", checkList.ReemplazoFiltroAceite);
                            AgregarCheckItem(col, "Filtro Aire Motor", checkList.ReemplazoFiltroAireMotor);
                            AgregarCheckItem(col, "Filtro Aire Polen", checkList.ReemplazoFiltroAirePolen);
                        });

                    row.ConstantItem(20);

                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).Column(col =>
                        {
                            col.Item().Background(Colors.Red.Darken2).Padding(3).Text("TRABAJOS REALIZADOS").FontSize(10).Bold().FontColor(Colors.White);
                            AgregarCheckItem(col, "Descristalización de Discos/Tambores de Freno", checkList.DescristalizacionTamboresDiscos);
                            AgregarCheckItem(col, "Ajuste de Frenos", checkList.AjusteFrenos);
                            AgregarCheckItem(col, "Calibración de Presión de Neumaticos", checkList.CalibracionPresionNeumaticos);
                            AgregarCheckItem(col, "Torque de birlos de rueda", checkList.TorqueNeumaticos);
                            AgregarCheckItem(col, "Rotación de Neumáticos", checkList.RotacionNeumaticos);
                        });
                });
            });
        }

        private void AgregarSeccionCheckList(TableDescriptor table, string titulo,
            params (string nombre, string valor)[] items)
        {
            // Header de sección
            table.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten3).PaddingTop(5)
                .Padding(5).Text(titulo).FontSize(9).Bold();

            // Items en pares
            for (int i = 0; i < items.Length; i += 2)
            {
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                    .Padding(5).Text(items[i].nombre).FontSize(8);
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                    .Padding(5).Text(items[i].valor).FontSize(8).Bold();

                if (i + 1 < items.Length)
                {
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(5).Text(items[i + 1].nombre).FontSize(8);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(5).Text(items[i + 1].valor).FontSize(8).Bold();
                }
                else
                {
                    table.Cell().ColumnSpan(2).BorderBottom(1)
                        .BorderColor(Colors.Grey.Lighten2);
                }
            }
        }

        private void AgregarCheckItem(ColumnDescriptor column, string texto, bool valor)
        {
            column.Item().PaddingTop(3).Row(row =>
            {
                row.ConstantItem(15).Text(valor ? "✓" : "✗")
                    .FontColor(valor ? Colors.Green.Medium : Colors.Red.Medium);
                row.RelativeItem().Text(texto).FontSize(8);
            });
        }

        private void SeccionCostos(IContainer container, OrdenPdfDto orden)
        {
            container.Column(column =>
            {
                column.Item().AlignRight().Width(300).Border(2).EnsureSpace(300)
                    .BorderColor(Colors.Red.Darken2).Padding(15).Column(col =>
                    {
                        col.Item().Text("RESUMEN DE COSTOS").FontSize(12).Bold()
                                    .FontColor(Colors.Red.Darken2).AlignCenter();

                        col.Item().PaddingTop(10).LineHorizontal(1)
                                    .LineColor(Colors.Grey.Lighten2);

                        col.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Text("Refacciones:");
                            row.ConstantItem(100).AlignRight()
                                        .Text($"${orden.TotalRefacciones:N2}").Bold();
                        });

                        col.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text("Mano de Obra:");
                            row.ConstantItem(100).AlignRight()
                                        .Text($"${orden.TotalManoObra:N2}").Bold();
                        });

                        col.Item().PaddingTop(5).LineHorizontal(1)
                                .LineColor(Colors.Black);

                        col.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text("Subtotal:");
                            row.ConstantItem(100).AlignRight()
                                        .Text($"${orden.CostoTotal:N2}").Bold();
                        });

                        col.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text("IVA:");
                            row.ConstantItem(100).AlignRight()
                                        .Text($"${orden.CostoTotal * 0.16m :N2}").Bold();
                        });

                        col.Item().PaddingTop(10).LineHorizontal(2)
                                    .LineColor(Colors.Red.Darken2);

                        col.Item().PaddingTop(10).Background(Colors.Red.Darken2)
                                    .Padding(8).Row(row =>
                        {
                            row.RelativeItem().Text("TOTAL")
                                        .FontSize(13).Bold().FontColor(Colors.White);
                            row.ConstantItem(100).AlignRight()
                                        .Text($"${orden.CostoTotal_IVA:N2}")
                                        .FontSize(14).Bold().FontColor(Colors.White);
                        });
                    });
            });
        }

        private void SeccionObservaciones(IContainer container, OrdenPdfDto orden)
        {
            container.Column(column =>
            {
                if (!string.IsNullOrWhiteSpace(orden.ObservacionesAsesor))
                {
                    column.Item().Border(1).BorderColor(Colors.Orange.Lighten2)
                        .Padding(10).Column(col =>
                        {
                            col.Item().Text("OBSERVACIONES DEL ASESOR")
                                        .FontSize(10).Bold().FontColor(Colors.Orange.Darken2);
                            col.Item().PaddingTop(5).Text(orden.ObservacionesAsesor)
                                        .FontSize(9);
                        });
                }

                if (!string.IsNullOrWhiteSpace(orden.ObservacionesJefeTaller))
                {
                    column.Item().PaddingTop(10).Border(1)
                        .BorderColor(Colors.Blue.Lighten2).Padding(10).Column(col =>
                        {
                            col.Item().Text("OBSERVACIONES DEL JEFE DE TALLER")
                                        .FontSize(10).Bold().FontColor(Colors.Blue.Darken2);
                            col.Item().PaddingTop(5).Text(orden.ObservacionesJefeTaller)
                                        .FontSize(9);
                        });
                }
            });
        }

        private void CrearPiePagina(IContainer container, OrdenPdfDto orden)
        {
            container.AlignBottom().Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Text(txt =>
                    {
                        txt.Span("Generado: ").FontSize(8);
                        txt.Span(DateTime.Now.ToString("dd/MMM/yyyy HH:mm"))
                            .FontSize(8).Bold();
                    });

                    row.ConstantItem(100).AlignRight().Text(txt =>
                    {
                        txt.Span("Página ").FontSize(8);
                        txt.CurrentPageNumber().FontSize(8).Bold();
                        txt.Span(" de ").FontSize(8);
                        txt.TotalPages().FontSize(8).Bold();
                    });
                });
            });
        }
    }
}