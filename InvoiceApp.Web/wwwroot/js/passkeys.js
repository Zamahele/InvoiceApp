(function () {
    function decodeBase64Url(value) {
        var base64 = value.replace(/-/g, '+').replace(/_/g, '/');
        base64 += '='.repeat((4 - base64.length % 4) % 4);
        var binary = atob(base64);
        return Uint8Array.from(binary, function (character) { return character.charCodeAt(0); });
    }

    function encodeBase64Url(buffer) {
        var bytes = new Uint8Array(buffer);
        var binary = '';
        bytes.forEach(function (value) { binary += String.fromCharCode(value); });
        return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
    }

    function creationOptions(options) {
        options.challenge = decodeBase64Url(options.challenge);
        options.user.id = decodeBase64Url(options.user.id);
        (options.excludeCredentials || []).forEach(function (credential) {
            credential.id = decodeBase64Url(credential.id);
        });
        return options;
    }

    function requestOptions(options) {
        options.challenge = decodeBase64Url(options.challenge);
        (options.allowCredentials || []).forEach(function (credential) {
            credential.id = decodeBase64Url(credential.id);
        });
        return options;
    }

    function serializeCredential(credential) {
        var response = credential.response;
        var serializedResponse = {
            clientDataJSON: encodeBase64Url(response.clientDataJSON)
        };

        if (response.attestationObject) {
            serializedResponse.attestationObject = encodeBase64Url(response.attestationObject);
            serializedResponse.transports = response.getTransports ? response.getTransports() : [];
        } else {
            serializedResponse.authenticatorData = encodeBase64Url(response.authenticatorData);
            serializedResponse.signature = encodeBase64Url(response.signature);
            serializedResponse.userHandle = response.userHandle ? encodeBase64Url(response.userHandle) : null;
        }

        return {
            id: credential.id,
            rawId: encodeBase64Url(credential.rawId),
            type: credential.type,
            response: serializedResponse,
            clientExtensionResults: credential.getClientExtensionResults(),
            authenticatorAttachment: credential.authenticatorAttachment
        };
    }

    function antiForgeryToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    }

    async function post(url, body, isJson) {
        var headers = { 'RequestVerificationToken': antiForgeryToken() };
        if (isJson) headers['Content-Type'] = 'application/json';
        else headers['Content-Type'] = 'application/x-www-form-urlencoded';

        var response = await fetch(url, {
            method: 'POST',
            headers: headers,
            body: isJson ? JSON.stringify(body) : body.toString()
        });
        var result = await response.json();
        if (!response.ok || result.success === false)
            throw new Error(result.message || 'Passkey request failed.');
        return result;
    }

    window.PasskeyClient = {
        antiForgeryToken: antiForgeryToken,
        createOptions: creationOptions,
        requestOptions: requestOptions,
        serializeCredential: serializeCredential,
        post: post
    };
})();
