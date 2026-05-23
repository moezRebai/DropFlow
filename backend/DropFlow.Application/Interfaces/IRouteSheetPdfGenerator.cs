using DropFlow.Application.Dto;

namespace DropFlow.Application.Interfaces;

/// <summary>
/// Service pour générer les feuilles de route en PDF
/// </summary>
public interface IRouteSheetPdfGenerator
{
    /// <summary>
    /// Génère une feuille de route en PDF
    /// </summary>
    /// <param name="data">Données de la route</param>
    /// <returns>Le PDF sous forme de tableau de bytes</returns>
    byte[] Generate(RouteSheetDto data);
    
    /// <summary>
    /// Génère une feuille de route en PDF de manière asynchrone
    /// </summary>
    /// <param name="data">Données de la route</param>
    /// <returns>Le PDF sous forme de tableau de bytes</returns>
    Task<byte[]> GenerateAsync(RouteSheetDto data);
}