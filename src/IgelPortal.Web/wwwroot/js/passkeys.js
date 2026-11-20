"use strict";

const passkeyForm = document.querySelector("[data-passkey-form]");
if (passkeyForm) {
    const supported = window.isSecureContext && window.PublicKeyCredential && navigator.credentials;
    const button = passkeyForm.querySelector('button[type="submit"]');
    const create = passkeyForm.dataset.passkeyForm === "create";

    passkeyForm.hidden = !supported;
    document.querySelector("[data-passkey-unavailable]").hidden = !!supported;

    if (supported) {
        button.disabled = false;
        passkeyForm.addEventListener("submit", async event => {
            event.preventDefault();
            if (button.disabled) {
                return;
            }

            const error = passkeyForm.querySelector("[data-passkey-error]");
            error.hidden = true;
            button.disabled = true;
            passkeyForm.setAttribute("aria-busy", "true");

            try {
                const token = passkeyForm.elements.namedItem("__RequestVerificationToken").value;
                const response = await fetch(passkeyForm.dataset.optionsUrl, {
                    method: "POST",
                    body: new URLSearchParams({ __RequestVerificationToken: token })
                });
                if (response.redirected) {
                    window.location.assign(response.url);
                    return;
                }
                if (!response.ok) {
                    throw new Error("Passkey options unavailable");
                }

                const options = await response.json();
                const publicKey = parsePasskeyOptions(options, create);
                const credential = create
                    ? await navigator.credentials.create({ publicKey })
                    : await navigator.credentials.get({ publicKey });
                if (!credential) {
                    throw new Error("No passkey returned");
                }

                passkeyForm.elements.namedItem("credential").value = JSON.stringify(passkeyToJson(credential, create));
                passkeyForm.submit();
            } catch {
                error.textContent = passkeyForm.dataset.error;
                error.hidden = false;
                button.disabled = false;
                passkeyForm.removeAttribute("aria-busy");
                button.focus();
            }
        });
    }
}

function parsePasskeyOptions(options, create) {
    if (create && PublicKeyCredential.parseCreationOptionsFromJSON) {
        return PublicKeyCredential.parseCreationOptionsFromJSON(options);
    }
    if (!create && PublicKeyCredential.parseRequestOptionsFromJSON) {
        return PublicKeyCredential.parseRequestOptionsFromJSON(options);
    }

    options.challenge = fromBase64Url(options.challenge);
    if (create) {
        options.user.id = fromBase64Url(options.user.id);
    }
    for (const credential of options.excludeCredentials ?? options.allowCredentials ?? []) {
        credential.id = fromBase64Url(credential.id);
    }
    return options;
}

function passkeyToJson(credential, create) {
    if (credential.toJSON) {
        return credential.toJSON();
    }

    const response = credential.response;
    const clientDataJSON = toBase64Url(response.clientDataJSON);
    return {
        id: credential.id,
        rawId: toBase64Url(credential.rawId),
        type: credential.type,
        clientExtensionResults: credential.getClientExtensionResults(),
        response: create
            ? { clientDataJSON, attestationObject: toBase64Url(response.attestationObject), transports: response.getTransports?.() }
            : {
                clientDataJSON,
                authenticatorData: toBase64Url(response.authenticatorData),
                signature: toBase64Url(response.signature),
                userHandle: response.userHandle ? toBase64Url(response.userHandle) : null
            }
    };
}

function fromBase64Url(value) {
    return Uint8Array.from(atob(value.replaceAll("-", "+").replaceAll("_", "/")), char => char.charCodeAt(0));
}

function toBase64Url(buffer) {
    return btoa(Array.from(new Uint8Array(buffer), byte => String.fromCharCode(byte)).join(""))
        .replaceAll("+", "-").replaceAll("/", "_").replace(/=+$/, "");
}
