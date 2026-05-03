// ════════════════════════════════════════════════════════════════
// GOOGLE MAPS AUTOCOMPLETE - VERSION ROBUSTE + FIX MUDBLAZOR DIALOG
// Trouve l'input même sans ID explicite + Fix z-index pour dialog
// ════════════════════════════════════════════════════════════════

export function initializeAutocomplete(dotNetRef, fieldId) {
    console.log('🗺️ Initializing Google Maps Autocomplete for field:', fieldId);

    // Vérifier que Google Maps est chargé
    if (!window.google || !window.google.maps || !window.google.maps.places) {
        console.error('❌ Google Maps API not loaded. Ensure the script is included in _Host.cshtml');
        return;
    }

    console.log('✅ Google Maps API is loaded');

    // ✅ MÉTHODES MULTIPLES POUR TROUVER L'INPUT
    let input = null;

    // Méthode 1 : Par ID exact
    console.log('🔍 Method 1: Searching by exact ID...');
    input = document.getElementById(fieldId);
    if (input) {
        console.log('✅ Found by exact ID:', input);
    }

    // Méthode 2 : Par ID partiel (pour IDs générés par Blazor)
    if (!input) {
        console.log('🔍 Method 2: Searching by partial ID...');
        input = document.querySelector(`input[id*="${fieldId}"]`);
        if (input) {
            console.log('✅ Found by partial ID:', input);
        }
    }

    // Méthode 3 : Par placeholder
    if (!input) {
        console.log('🔍 Method 3: Searching by placeholder...');
        input = document.querySelector('input[placeholder*="Commencez à saisir"]');
        if (input) {
            console.log('✅ Found by placeholder:', input);
        }
    }

    // Méthode 4 : Par aria-label
    if (!input) {
        console.log('🔍 Method 4: Searching by aria-label...');
        input = document.querySelector('input[aria-label*="Adresse"]');
        if (input) {
            console.log('✅ Found by aria-label:', input);
        }
    }

    // Méthode 5 : Chercher dans les MudTextField avec label contenant "Adresse"
    if (!input) {
        console.log('🔍 Method 5: Searching near label containing "Adresse"...');
        const labels = document.querySelectorAll('label');
        for (const label of labels) {
            if (label.textContent.includes('Adresse')) {
                // Chercher l'input suivant
                const container = label.closest('.mud-input-control');
                if (container) {
                    input = container.querySelector('input[type="text"]');
                    if (input) {
                        console.log('✅ Found by label proximity:', input);
                        break;
                    }
                }
            }
        }
    }

    // Méthode 6 : Par classe MudBlazor
    if (!input) {
        console.log('🔍 Method 6: Searching by MudBlazor class...');
        const mudInputs = document.querySelectorAll('.mud-input-slot input[type="text"]');
        // Trouver celui avec placeholder "Commencez"
        for (const mudInput of mudInputs) {
            if (mudInput.placeholder && mudInput.placeholder.includes('Commencez')) {
                input = mudInput;
                console.log('✅ Found by MudBlazor class:', input);
                break;
            }
        }
    }

    // Si aucune méthode n'a fonctionné
    if (!input) {
        console.error(`❌ Input field not found for ID: ${fieldId}`);
        console.log('📋 Available text inputs:', document.querySelectorAll('input[type="text"]'));
        console.log('📋 All inputs:', document.querySelectorAll('input'));
        console.log('💡 Try adding id="address-field" to your MudTextField');
        return;
    }

    console.log('✅ Input found successfully:', input);
    console.log('📍 Input details:', {
        id: input.id,
        name: input.name,
        placeholder: input.placeholder,
        'aria-label': input.getAttribute('aria-label')
    });

    // Options de l'autocomplete
    const options = {
        types: ['address'],
        componentRestrictions: { country: 'fr' },
        fields: ['formatted_address', 'address_components', 'geometry']
    };

    console.log('🔧 Creating Google Maps Autocomplete...');

    // Créer l'instance Google Maps Autocomplete
    const autocomplete = new google.maps.places.Autocomplete(input, options);

    console.log('✅ Autocomplete created successfully');

    // ════════════════════════════════════════════════════════════════
    // ✅ FIX POUR MUDDIALOG : Afficher les suggestions
    // ════════════════════════════════════════════════════════════════

    // FIX 1: Forcer autocomplete="new-password" pour contourner restrictions navigateur
    input.setAttribute('autocomplete', 'new-password');
    console.log('✅ Fixed autocomplete attribute');

    // FIX 2: Z-index élevé pour le dropdown Google Maps (au-dessus du MudDialog)
    const fixPacContainerZIndex = () => {
        const pacContainer = document.querySelector('.pac-container');
        if (pacContainer) {
            pacContainer.style.zIndex = '99999';
            pacContainer.style.position = 'fixed';
            console.log('✅ PAC container z-index fixed for MudDialog');
        } else {
            console.log('⏳ PAC container not found yet, will retry on input focus');
        }
    };

    // Appliquer le fix immédiatement
    setTimeout(fixPacContainerZIndex, 100);

    // Réappliquer le fix à chaque focus de l'input (au cas où le DOM change)
    input.addEventListener('focus', () => {
        setTimeout(fixPacContainerZIndex, 50);
    });

    // ════════════════════════════════════════════════════════════════
    // Écouter la sélection d'une adresse
    // ════════════════════════════════════════════════════════════════
    autocomplete.addListener('place_changed', () => {
        const place = autocomplete.getPlace();

        console.log('📍 Place selected:', place);

        if (!place.geometry || !place.geometry.location) {
            console.error('❌ No geometry for selected place');
            return;
        }

        // Extraire les composants de l'adresse
        const addressComponents = {};

        place.address_components.forEach(component => {
            const types = component.types;

            if (types.includes('street_number')) {
                addressComponents.streetNumber = component.long_name;
            }
            if (types.includes('route')) {
                addressComponents.route = component.long_name;
            }
            if (types.includes('locality')) {
                addressComponents.city = component.long_name;
            }
            if (types.includes('postal_code')) {
                addressComponents.postalCode = component.long_name;
            }
            if (types.includes('country')) {
                addressComponents.country = component.long_name;
            }
        });

        console.log('📊 Address components extracted:', addressComponents);

        // Construire l'adresse de rue
        const streetAddress = [
            addressComponents.streetNumber,
            addressComponents.route
        ].filter(Boolean).join(' ');

        // Construire le résultat pour Blazor
        const result = {
            formattedAddress: place.formatted_address,
            streetAddress: streetAddress || place.formatted_address,
            city: addressComponents.city || '',
            postalCode: addressComponents.postalCode || '',
            latitude: place.geometry.location.lat(),
            longitude: place.geometry.location.lng()
        };

        console.log('✅ Sending result to Blazor:', result);

        // Envoyer au composant Blazor
        dotNetRef.invokeMethodAsync('OnAddressSelected', result)
            .then(() => {
                console.log('✅ Address sent to Blazor successfully');
            })
            .catch(error => {
                console.error('❌ Error calling Blazor method:', error);
            });
    });

    console.log('✅ Event listener attached - Autocomplete is ready! 🎉');
}

/**
 * Nettoyer l'autocomplete
 */
export function disposeAutocomplete() {
    console.log('🧹 Disposing Google Maps Autocomplete');
}